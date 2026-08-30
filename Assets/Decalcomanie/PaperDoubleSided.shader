Shader "Custom/PaperDoubleSided"
{
    Properties
    {
        _FrontTex     ("Front Texture", 2D) = "white" {}
        _FrontColor   ("Front Color", Color) = (1,1,1,1)
        _PaperColor   ("Paper Color (Back)", Color) = (0.94,0.93,0.89,1)
        _BleedThrough ("Ink Bleed Through", Range(0,0.5)) = 0.12
        _Translucency ("Translucency (Backlight)", Range(0,1)) = 0.15
        _Smoothness   ("Smoothness", Range(0,1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        // 종이의 핵심: 양면 모두 그린다.
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            TEXTURE2D(_FrontTex);  SAMPLER(sampler_FrontTex);

            // SRP Batcher 호환을 위해 반드시 CBUFFER로 묶는다.
            CBUFFER_START(UnityPerMaterial)
                float4 _FrontTex_ST;
                half4  _FrontColor;
                half4  _PaperColor;
                half   _BleedThrough;
                half   _Translucency;
                half   _Smoothness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half isFront = IS_FRONT_VFACE(face, 1.0h, 0.0h);

                // 백페이스는 노멀을 뒤집어야 조명이 정상적으로 들어온다.
                float3 normalWS = normalize(IN.normalWS) * (isFront * 2.0 - 1.0);

                float2 uv = IN.uv * _FrontTex_ST.xy + _FrontTex_ST.zw;
                half4 ink = SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, uv) * _FrontColor;

                // 앞면: 그림 그대로.
                // 뒷면: 종이색 단색 + 앞면 잉크가 아주 희미하게 비쳐 보이는 정도.
                //       UV는 같은 좌표를 그대로 쓴다. 같은 지점의 반대편이므로
                //       좌우 반전은 화면 투영 단계에서 이미 일어나 있다.
                half3 backside = lerp(_PaperColor.rgb, _PaperColor.rgb * ink.rgb, _BleedThrough);
                half3 albedo   = lerp(backside, ink.rgb, isFront);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = albedo;
                surface.alpha      = 1.0h;
                surface.metallic   = 0.0h;
                surface.smoothness = _Smoothness;
                surface.occlusion  = 1.0h;
                surface.normalTS   = half3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS      = IN.positionWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord        = IN.fogFactor;
                inputData.bakedGI         = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(inputData, surface);

                // 얇은 종이의 투과광: 빛이 반대편에서 들어올 때 은은하게 밝아진다.
                if (_Translucency > 0.001h)
                {
                    Light mainLight = GetMainLight();
                    half transmit = saturate(dot(-normalWS, mainLight.direction));
                    transmit = transmit * transmit; // 뒤쪽에 가까울수록만 살아나게
                    color.rgb += albedo * mainLight.color * transmit
                                 * mainLight.distanceAttenuation * _Translucency;
                }

                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SVaryings   { float4 positionCS : SV_POSITION; };

            SVaryings shadowVert (SAttributes IN)
            {
                SVaryings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDir));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 shadowFrag (SVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DAttributes { float4 positionOS : POSITION; };
            struct DVaryings   { float4 positionCS : SV_POSITION; };

            DVaryings depthVert (DAttributes IN)
            {
                DVaryings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 depthFrag (DVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
