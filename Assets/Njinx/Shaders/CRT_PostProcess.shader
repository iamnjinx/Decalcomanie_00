Shader "Hidden/CRT_PostProcess"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        [Header(Color)]
        _Saturation ("Saturation", Range(0, 2)) = 0.7
        _Brightness ("Brightness", Range(0.5, 3)) = 1.2
        _EmissionBoost ("Emission Boost", Range(1, 10)) = 2.0

        [Header(Scanline)]
        _ScanlineCount ("Scanline Count", Range(50, 1000)) = 300
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3

        [Header(Shadow Mask)]
        _MaskScale ("Mask Scale", Range(50, 2000)) = 800
        _MaskIntensity ("Mask Intensity", Range(0, 1)) = 0.25

        [Header(Noise)]
        _NoiseAmount ("Noise Amount", Range(0, 0.15)) = 0.03

        [Header(Vignette)]
        _VignetteStrength ("Vignette Strength", Range(0, 2)) = 0.8
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01, 1)) = 0.4

        [Header(Curvature)]
        _CurvatureAmount ("Barrel Distortion", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CRT"
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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Saturation;
                float _Brightness;
                float _EmissionBoost;
                float _ScanlineCount;
                float _ScanlineIntensity;
                float _MaskScale;
                float _MaskIntensity;
                float _NoiseAmount;
                float _VignetteStrength;
                float _VignetteSmoothness;
                float _CurvatureAmount;
            CBUFFER_END

            // ------------------------------------------------
            // 배럴 디스토션 (CRT 볼록한 화면)
            // ------------------------------------------------
            float2 BarrelDistortion(float2 uv, float amount)
            {
                float2 centered = uv * 2.0 - 1.0;
                float r2 = dot(centered, centered);
                centered *= 1.0 + amount * r2;
                return centered * 0.5 + 0.5;
            }

            // ------------------------------------------------
            // 해시 노이즈 (블렌더 White Noise Texture)
            // ------------------------------------------------
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // ------------------------------------------------
            // HSV 조정 (블렌더 Hue/Saturation/Value)
            // ------------------------------------------------
            float3 AdjustSatBright(float3 col, float sat, float val)
            {
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                return lerp(float3(luma, luma, luma), col, sat) * val;
            }

            // ------------------------------------------------
            // 비네트 (블렌더 UV 거리 기반)
            // ------------------------------------------------
            float Vignette(float2 uv, float strength, float smooth)
            {
                float2 c = uv * 2.0 - 1.0;
                float d = length(c);
                return 1.0 - smoothstep(1.0 - smooth, 1.0, d * strength);
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. 배럴 디스토션
                float2 distUV = BarrelDistortion(uv, _CurvatureAmount);

                // 화면 밖이면 검정
                if (distUV.x < 0 || distUV.x > 1 || distUV.y < 0 || distUV.y > 1)
                    return half4(0, 0, 0, 1);

                // 2. 소스 텍스처 샘플링
                float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distUV).rgb;

                // 3. 채도/밝기 조정
                col = AdjustSatBright(col, _Saturation, _Brightness);

                // 4. 노이즈 믹스
                float n = Hash(distUV * 1000.0 + _Time.y * 60.0);
                col = lerp(col, float3(n, n, n), _NoiseAmount);

                // 5. 스캔라인 (블렌더 Wave Texture)
                float scanline = sin(distUV.y * _ScanlineCount * PI * 2.0) * 0.5 + 0.5;
                scanline = smoothstep(0.3, 0.7, scanline);
                col *= lerp(1.0, scanline, _ScanlineIntensity);

                // 6. 섀도우 마스크 - RGB 서브픽셀
                //    (블렌더 Wave Texture.001 + .002 → Separate/Combine)
                float2 pixelCoord = distUV * _ScreenParams.xy;
                int subpixel = (int)fmod(pixelCoord.x, 3.0);
                float3 mask = float3(0.33, 0.33, 0.33);
                if (subpixel == 0) mask.r = 1.0;
                else if (subpixel == 1) mask.g = 1.0;
                else mask.b = 1.0;

                // 수직 마스크 줄도 추가
                float vMask = sin(pixelCoord.y * PI) * 0.5 + 0.5;
                vMask = lerp(1.0, vMask, 0.3);
                mask *= vMask;

                col *= lerp(float3(1, 1, 1), mask, _MaskIntensity);

                // 7. 비네트
                float vig = Vignette(distUV, _VignetteStrength, _VignetteSmoothness);
                col *= vig;

                // 8. 에미션 부스트
                col *= _EmissionBoost;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
