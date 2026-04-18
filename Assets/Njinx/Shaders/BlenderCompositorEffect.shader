Shader "Custom/BlenderCompositorEffect"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BlendColor ("Blend Color", Color) = (0.45, 0.2, 0.55, 1) // 컬러 노드의 색상
        _SoftLightFactor ("Soft Light Factor", Range(0, 1)) = 0.392
        _PosterizeSteps ("Posterize Steps", Float) = 15.0
        _ChromaticFactor ("Chromatic Aberration", Float) = 0.122
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "BlenderCompositor"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float4 _BlendColor;
            float _SoftLightFactor;
            float _PosterizeSteps;
            float _ChromaticFactor;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            // ===== 1단계: Soft Light 블렌딩 =====
            // Photoshop/Blender 방식의 Soft Light
            float SoftLight(float base, float blend)
            {
                return (blend < 0.5)
                    ? 2.0 * base * blend + base * base * (1.0 - 2.0 * blend)
                    : sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend);
            }

            float3 ApplySoftLight(float3 base, float3 blend, float factor)
            {
                float3 result;
                result.r = SoftLight(base.r, blend.r);
                result.g = SoftLight(base.g, blend.g);
                result.b = SoftLight(base.b, blend.b);
                return lerp(base, result, factor);
            }

            // ===== 2단계: Posterize =====
            float3 Posterize(float3 col, float steps)
            {
                return floor(col * steps) / steps;
            }

            // ===== 3단계: Chromatic Aberration (Horizontal) =====
            float3 ChromaticAberration(float2 uv, float factor)
            {
                float offset = factor * _MainTex_TexelSize.x * 10.0;
                // 주의: 여기서는 Posterize 결과를 다시 샘플링할 수 없으므로
                // 입력 텍스처 기준으로 오프셋 샘플링
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset, 0)).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(offset, 0)).b;
                return float3(r, g, b);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Chromatic Aberration을 먼저 샘플링 단계에서 적용
                float offset = _ChromaticFactor * _MainTex_TexelSize.x * 10.0;

                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                           i.uv + float2(offset, 0)).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                           i.uv - float2(offset, 0)).b;

                float3 col = float3(r, g, b);

                // 1) Soft Light
                col = ApplySoftLight(col, _BlendColor.rgb, _SoftLightFactor);

                // 2) Posterize
                col = Posterize(col, _PosterizeSteps);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}