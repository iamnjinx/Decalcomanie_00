using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CompositorEffectFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();

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

            // source → temp (셰이더 적용)
            cmd.Blit(source, tempRTId, mat, 0);
            // temp → source (결과를 카메라에 복사)
            cmd.Blit(tempRTId, source);

            cmd.ReleaseTemporaryRT(tempRTId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    CompositorPass pass;

    public override void Create()
    {
        if (settings.material != null)
        {
            pass = new CompositorPass(settings.material);
            pass.renderPassEvent = settings.renderPassEvent;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass != null)
            renderer.EnqueuePass(pass);
    }
}