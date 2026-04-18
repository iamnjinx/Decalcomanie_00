Shader "Hidden/StencilWrite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // 색은 안 쓰고 Stencil에만 1을 기록
            ColorMask 0
            ZWrite Off
            ZTest LEqual

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0; // 색 출력 없음 (ColorMask 0)
            }
            ENDHLSL
        }
    }
}
