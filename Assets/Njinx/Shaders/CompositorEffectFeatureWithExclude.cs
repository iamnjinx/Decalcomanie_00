using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CompositorEffectFeatureWithExclude : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Tooltip("이펙트에서 제외할 레이어 (비워두면 전체 적용)")]
        public LayerMask excludeLayer;

        [Tooltip("제외 레이어 기능 사용 여부")]
        public bool useExcludeLayer = false;
    }

    public Settings settings = new Settings();

    // =========================================================
    // 기존 CompositorPass 그대로
    // =========================================================
    class CompositorPass : ScriptableRenderPass
    {
        Material mat;
        int tempRTId = Shader.PropertyToID("_TempCompositorRT");

        public CompositorPass(Material material)
        {
            mat = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (mat == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("CompositorEffect");

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
    // 제외 레이어 다시 그리기 패스
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

    CompositorPass pass;
    RedrawExcludePass redrawPass;

    public override void Create()
    {
        if (settings.material != null)
        {
            pass = new CompositorPass(settings.material);
            pass.renderPassEvent = settings.renderPassEvent;

            if (settings.useExcludeLayer)
            {
                redrawPass = new RedrawExcludePass(settings.excludeLayer);
                redrawPass.renderPassEvent = settings.renderPassEvent + 1;
            }
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass != null)
        {
            renderer.EnqueuePass(pass);

            if (settings.useExcludeLayer && redrawPass != null)
                renderer.EnqueuePass(redrawPass);
        }
    }
}
