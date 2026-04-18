Shader "Custom/OutlineFullscreen"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1)
        _OutlineWidth ("Outline Width", Range(1, 10)) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "OutlinePass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            TEXTURE2D(_StencilMaskTex);
            SAMPLER(sampler_StencilMaskTex);

            float4 _OutlineColor;
            float _OutlineWidth;

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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;

                float center = SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv).r;

                // 8방향 샘플링
                float mask = 0;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2( texelSize.x, 0)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2(-texelSize.x, 0)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2(0,  texelSize.y)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2(0, -texelSize.y)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2( texelSize.x,  texelSize.y)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2(-texelSize.x,  texelSize.y)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2( texelSize.x, -texelSize.y)).r;
                mask += SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, input.uv + float2(-texelSize.x, -texelSize.y)).r;

                // 주변에 마스크 있고 + 현재 위치는 마스크 밖 = 아웃라인
                float outline = step(0.01, mask) * (1.0 - center);

                float4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return lerp(sceneColor, _OutlineColor, outline);
            }
            ENDHLSL
        }
    }
}