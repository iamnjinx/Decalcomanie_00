using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ExcludeLayerCompositorFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("적용할 포스트 이펙트 머티리얼 (CRT 쉐이더 등)")]
        public Material effectMaterial;

        [Tooltip("이펙트에서 제외할 레이어")]
        public LayerMask excludeLayer;

        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    // =========================================================
    // Pass 1: 제외 레이어 오브젝트를 Stencil 버퍼에 마킹
    // =========================================================
    class StencilMarkPass : ScriptableRenderPass
    {
        FilteringSettings filteringSettings;
        ShaderTagId shaderTag = new ShaderTagId("UniversalForward");
        Material stencilWriteMat;

        public StencilMarkPass(LayerMask excludeLayer)
        {
            filteringSettings = new FilteringSettings(RenderQueueRange.all, excludeLayer);

            // Stencil에 1을 쓰는 머티리얼
            stencilWriteMat = CoreUtils.CreateEngineMaterial(
                Shader.Find("Hidden/StencilWrite"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (stencilWriteMat == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("StencilMark");

            // 제외 레이어 오브젝트를 stencilWriteMat으로 다시 그려서 스텐실 마킹
            var drawSettings = CreateDrawingSettings(
                shaderTag, ref renderingData, SortingCriteria.CommonOpaque);
            drawSettings.overrideMaterial = stencilWriteMat;
            drawSettings.overrideMaterialPassIndex = 0;

            context.DrawRenderers(
                renderingData.cullResults, ref drawSettings, ref filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            if (stencilWriteMat != null)
                CoreUtils.Destroy(stencilWriteMat);
        }
    }

    // =========================================================
    // Pass 2: Stencil 마킹 안 된 곳에만 이펙트 Blit
    // =========================================================
    class ExcludeBlitPass : ScriptableRenderPass
    {
        Material effectMat;
        int tempRTId = Shader.PropertyToID("_TempExcludeRT");

        public ExcludeBlitPass(Material material)
        {
            effectMat = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (effectMat == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("ExcludeBlit");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            var cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

            cmd.GetTemporaryRT(tempRTId, desc);

            // 이펙트 적용 (전체에 한번 Blit)
            cmd.Blit(cameraColor, tempRTId, effectMat, 0);

            // Stencil 값이 1이 아닌 곳만 결과를 복사 (제외 레이어는 원본 유지)
            // → StencilCompose 쉐이더가 처리
            cmd.SetGlobalTexture("_EffectedTex", tempRTId);
            var composeMat = CoreUtils.CreateEngineMaterial(
                Shader.Find("Hidden/StencilCompose"));
            cmd.Blit(cameraColor, cameraColor, composeMat, 0);

            cmd.ReleaseTemporaryRT(tempRTId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    StencilMarkPass markPass;
    ExcludeBlitPass blitPass;

    public override void Create()
    {
        if (settings.effectMaterial == null) return;

        markPass = new StencilMarkPass(settings.excludeLayer);
        markPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        blitPass = new ExcludeBlitPass(settings.effectMaterial);
        blitPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (markPass != null && blitPass != null)
        {
            renderer.EnqueuePass(markPass);
            renderer.EnqueuePass(blitPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        markPass?.Cleanup();
    }
}
