using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTRendererFeature2 : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Header("Color")]
        [Range(0, 2)] public float saturation = 0.7f;
        [Range(0.5f, 3)] public float brightness = 1.2f;
        [Range(1, 10)] public float emissionBoost = 2.0f;

        [Header("Scanline")]
        [Range(50, 1000)] public float scanlineCount = 300;
        [Range(0, 1)] public float scanlineIntensity = 0.3f;

        [Header("Shadow Mask")]
        [Range(50, 2000)] public float maskScale = 800;
        [Range(0, 1)] public float maskIntensity = 0.25f;

        [Header("Noise")]
        [Range(0, 0.15f)] public float noiseAmount = 0.03f;

        [Header("Vignette")]
        [Range(0, 2)] public float vignetteStrength = 0.8f;
        [Range(0.01f, 1)] public float vignetteSmoothness = 0.4f;

        [Header("Curvature")]
        [Range(0, 0.1f)] public float curvatureAmount = 0.02f;

        [Header("Exclude Layer (Optional)")]
        public bool useExcludeLayer = false;
        public LayerMask excludeLayer;
    }

    public Settings settings = new Settings();

    Material crtMaterial;

    // =========================================================
    // CRT Blit Pass
    // =========================================================
    class CRTPass : ScriptableRenderPass
    {
        Material mat;
        Settings cfg;
        int tempRTId = Shader.PropertyToID("_TempCRTRT");

        public CRTPass(Material material, Settings settings)
        {
            mat = material;
            cfg = settings;
        }

        public void UpdateSettings(Settings s)
        {
            cfg = s;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (mat == null) return;

            // 인스펙터 값을 쉐이더에 반영
            mat.SetFloat("_Saturation", cfg.saturation);
            mat.SetFloat("_Brightness", cfg.brightness);
            mat.SetFloat("_EmissionBoost", cfg.emissionBoost);
            mat.SetFloat("_ScanlineCount", cfg.scanlineCount);
            mat.SetFloat("_ScanlineIntensity", cfg.scanlineIntensity);
            mat.SetFloat("_MaskScale", cfg.maskScale);
            mat.SetFloat("_MaskIntensity", cfg.maskIntensity);
            mat.SetFloat("_NoiseAmount", cfg.noiseAmount);
            mat.SetFloat("_VignetteStrength", cfg.vignetteStrength);
            mat.SetFloat("_VignetteSmoothness", cfg.vignetteSmoothness);
            mat.SetFloat("_CurvatureAmount", cfg.curvatureAmount);

            CommandBuffer cmd = CommandBufferPool.Get("CRT Effect");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            var renderer = renderingData.cameraData.renderer;
            var source = renderer.cameraColorTargetHandle;

            cmd.GetTemporaryRT(tempRTId, desc);
            cmd.Blit(source, tempRTId, mat, 0);
            cmd.Blit(tempRTId, source);
            cmd.ReleaseTemporaryRT(tempRTId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    // =========================================================
    // Exclude Layer Redraw Pass
    // =========================================================
    class RedrawExcludePass : ScriptableRenderPass
    {
        FilteringSettings filteringSettings;
        ShaderTagId[] shaderTags;

        public RedrawExcludePass(LayerMask excludeLayer)
        {
            filteringSettings = new FilteringSettings(RenderQueueRange.all, excludeLayer);
            shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("RedrawExclude");
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            for (int i = 0; i < shaderTags.Length; i++)
            {
                var drawSettings = CreateDrawingSettings(
                    shaderTags[i], ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(
                    renderingData.cullResults, ref drawSettings, ref filteringSettings);
            }
        }
    }

    CRTPass crtPass;
    RedrawExcludePass redrawPass;

    public override void Create()
    {
        // 쉐이더에서 머티리얼 자동 생성
        var shader = Shader.Find("Hidden/CRT_PostProcess");
        if (shader == null)
        {
            Debug.LogError("CRTRendererFeature: Hidden/CRT_PostProcess 쉐이더를 찾을 수 없습니다.");
            return;
        }

        if (crtMaterial == null)
            crtMaterial = CoreUtils.CreateEngineMaterial(shader);

        crtPass = new CRTPass(crtMaterial, settings);
        crtPass.renderPassEvent = settings.renderPassEvent;

        if (settings.useExcludeLayer)
        {
            redrawPass = new RedrawExcludePass(settings.excludeLayer);
            redrawPass.renderPassEvent = settings.renderPassEvent + 1;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (crtPass != null)
        {
            crtPass.UpdateSettings(settings);
            renderer.EnqueuePass(crtPass);

            if (settings.useExcludeLayer && redrawPass != null)
                renderer.EnqueuePass(redrawPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (crtMaterial != null)
            CoreUtils.Destroy(crtMaterial);
    }
}
