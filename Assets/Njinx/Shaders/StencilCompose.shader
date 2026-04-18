Shader "Hidden/StencilCompose"
{
    Properties
    {
        _MainTex ("Original", 2D) = "white" {}
        _EffectedTex ("Effected", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always ZWrite Off Cull Off

            // Stencil 값이 1이 아닌 곳에서만 이 패스가 실행됨
            // = 제외 레이어가 아닌 곳에서만 이펙트 결과를 사용
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EffectedTex);
            SAMPLER(sampler_EffectedTex);

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Stencil NotEqual 1 → 여기는 제외 레이어가 아님 → 이펙트 적용
                return SAMPLE_TEXTURE2D(_EffectedTex, sampler_EffectedTex, input.uv);
            }
            ENDHLSL
        }
    }
}
