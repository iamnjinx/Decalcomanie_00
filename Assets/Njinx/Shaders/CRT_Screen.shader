Shader "Custom/CRT_Screen"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Screen Texture", 2D) = "white" {}
        
        [Header(Color Adjustment)]
        _Saturation ("Saturation", Range(0, 2)) = 0.7
        _Brightness ("Brightness (Value)", Range(0.5, 3)) = 1.2
        _EmissionStrength ("Emission Strength", Range(0, 20)) = 10.0
        
        [Header(Scanline)]
        _ScanlineScale ("Scanline Scale", Range(10, 200)) = 53.5
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _ScanlineScrollSpeed ("Scanline Scroll Speed", Range(0, 2)) = 0.3
        
        [Header(Shadow Mask RGB Subpixel)]
        _MaskScaleX ("Mask Scale X", Range(10, 200)) = 50.0
        _MaskScaleY ("Mask Scale Y", Range(10, 200)) = 50.0
        _MaskIntensity ("Mask Intensity", Range(0, 1)) = 0.5
        
        [Header(Shadow Mask Texture Optional)]
        _MaskTex ("Shadow Mask Texture", 2D) = "white" {}
        _MaskTexScale ("Mask Texture Scale", Range(1, 100)) = 10.0
        _UseMaskTex ("Use Mask Texture", Range(0, 1)) = 0.0
        
        [Header(Noise)]
        _NoiseAmount ("Noise Amount", Range(0, 0.3)) = 0.05
        
        [Header(Vignette)]
        _VignetteStrength ("Vignette Strength", Range(0, 3)) = 1.0
        _VignetteSmoothness ("Vignette Smoothness", Range(0.01, 1)) = 0.4
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "CRT_Forward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float _Saturation;
                float _Brightness;
                float _EmissionStrength;
                float _ScanlineScale;
                float _ScanlineIntensity;
                float _ScanlineScrollSpeed;
                float _MaskScaleX;
                float _MaskScaleY;
                float _MaskIntensity;
                float _MaskTexScale;
                float _UseMaskTex;
                float _NoiseAmount;
                float _VignetteStrength;
                float _VignetteSmoothness;
            CBUFFER_END
            
            // ============================================================
            // Utility Functions
            // ============================================================
            
            // 블렌더 Hue/Saturation/Value 노드 재현
            float3 AdjustHSV(float3 color, float saturation, float value)
            {
                // RGB -> HSV 변환 없이 간단한 saturation/brightness 조정
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                float3 saturated = lerp(float3(luma, luma, luma), color, saturation);
                return saturated * value;
            }
            
            // 블렌더 White Noise Texture 재현 - hash 기반
            float3 WhiteNoise(float2 uv)
            {
                float3 p = float3(uv, _Time.y * 30.0);
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p, p.yzx + 19.19);
                return frac(float3(
                    (p.x + p.y) * p.z,
                    (p.x + p.z) * p.y,
                    (p.y + p.z) * p.x
                ));
            }
            
            // 블렌더 Wave Texture (Bands, Sine) 재현
            // 블렌더의 Wave Texture는 기본적으로 sin 기반 밴드 패턴
            float WaveTexture(float2 uv, float scale, float scrollSpeed)
            {
                float wave = sin((uv.y - _Time.y * scrollSpeed) * scale * PI * 2.0);
                return wave * 0.5 + 0.5; // 0~1로 정규화
            }
            
            // 블렌더 Color Ramp 재현 (Linear, 흑백)
            // 입력 factor를 contrast 있게 매핑
            float ColorRampBW(float factor, float pos0, float pos1)
            {
                return smoothstep(pos0, pos1, factor);
            }
            
            // 블렌더 Vignette - UV 거리 기반
            float Vignette(float2 uv, float strength, float smoothness)
            {
                float2 centered = uv * 2.0 - 1.0;
                float dist = length(centered);
                float vig = 1.0 - smoothstep(1.0 - smoothness, 1.0, dist * strength);
                return vig;
            }
            
            // ============================================================
            // Vertex Shader
            // ============================================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            
            // ============================================================
            // Fragment Shader
            // ============================================================
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // -------------------------------------------------------
                // 1. 메인 텍스처 샘플링 + HSV 조정
                //    블렌더: Image Texture → Hue/Saturation/Value
                // -------------------------------------------------------
                float3 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                screenColor = AdjustHSV(screenColor, _Saturation, _Brightness);
                
                // -------------------------------------------------------
                // 2. 노이즈 믹스
                //    블렌더: White Noise → Mix.001 (HSV 결과와 노이즈 블렌딩)
                // -------------------------------------------------------
                float3 noise = WhiteNoise(uv);
                float3 withNoise = lerp(screenColor, noise, _NoiseAmount);
                
                // -------------------------------------------------------
                // 3. 스캔라인
                //    블렌더: Wave Texture (Scale 53.5) → Color Ramp → Mix
                //    수평 줄무늬 패턴
                // -------------------------------------------------------
                float scanWave = WaveTexture(uv, _ScanlineScale, _ScanlineScrollSpeed);
                float scanline = ColorRampBW(scanWave, 0.4, 0.6);
                
                // Mix: A=withNoise, B=scanline, Factor=_ScanlineIntensity
                float3 withScanline = lerp(withNoise, withNoise * scanline, _ScanlineIntensity);
                
                // -------------------------------------------------------
                // 4. 비네트
                //    블렌더: Texture Coordinate UV → Vector Math → Color Ramp.003
                //    → Mix.004 (스캔라인 결과와 비네트 블렌딩)
                // -------------------------------------------------------
                float vig = Vignette(uv, _VignetteStrength, _VignetteSmoothness);
                float3 withVignette = withScanline * vig;
                
                // -------------------------------------------------------
                // 5. 섀도우 마스크 (RGB 서브픽셀)
                //    블렌더: Wave Texture.001 (X방향) + Wave Texture.002 (Y방향)
                //    → Color Ramp들 → Mix.003 → Separate/Combine Color로
                //    채널별 곱셈
                //
                //    실제 CRT의 RGB 서브픽셀 그리드를 시뮬레이션
                // -------------------------------------------------------
                float3 finalColor;
                
                if (_UseMaskTex > 0.5)
                {
                    // 외부 섀도우 마스크 텍스처 사용
                    float2 maskUV = uv * _MaskTexScale;
                    float3 maskColor = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, maskUV).rgb;
                    
                    // 채널별 곱셈 (블렌더의 Separate → Math(Multiply) → Combine 구조)
                    finalColor.r = withVignette.r * maskColor.r;
                    finalColor.g = withVignette.g * maskColor.g;
                    finalColor.b = withVignette.b * maskColor.b;
                }
                else
                {
                    // 절차적 섀도우 마스크 생성
                    // 블렌더의 Wave Texture 2개로 만든 격자 패턴
                    float maskX = sin(uv.x * _MaskScaleX * PI * 2.0) * 0.5 + 0.5;
                    float maskY = sin(uv.y * _MaskScaleY * PI * 2.0) * 0.5 + 0.5;
                    
                    // Color Ramp으로 선명하게
                    maskX = smoothstep(0.3, 0.7, maskX);
                    maskY = smoothstep(0.3, 0.7, maskY);
                    
                    float mask = lerp(maskX, maskY, 0.5);
                    mask = lerp(1.0, mask, _MaskIntensity);
                    
                    finalColor = withVignette * mask;
                }
                
                // -------------------------------------------------------
                // 6. Emission 출력
                //    블렌더: Combine Color → Principled BSDF의 
                //    Base Color + Emission Color (Strength 10)
                //    CRT는 자체 발광이므로 Emission으로 처리
                // -------------------------------------------------------
                float3 emission = finalColor * _EmissionStrength;
                
                // 최종 출력: Base Color + Emission
                // URP에서 Unlit처럼 동작하되 emission 값을 살림
                return half4(finalColor + emission, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
