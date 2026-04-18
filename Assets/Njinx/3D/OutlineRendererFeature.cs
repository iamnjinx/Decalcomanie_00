using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask outlineLayerMask;
        public Color outlineColor = Color.yellow;
        [Range(1, 10)] public float outlineWidth = 3f;
        public Material outlineMaterial;
    }

    public Settings settings = new();

    private MaskPass _maskPass;
    private OutlinePass _outlinePass;

    public override void Create()
    {
        _maskPass = new MaskPass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
        _outlinePass = new OutlinePass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;
        renderer.EnqueuePass(_maskPass);
        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        _maskPass?.Dispose();
        _outlinePass?.Dispose();
    }

    // =============================================
    // Pass 1: 아웃라인 대상을 흰색 마스크로 렌더링
    // =============================================
    class MaskPass : ScriptableRenderPass
    {
        private Settings _settings;
        private RTHandle _maskRT;
        private Material _whiteMat;
        private readonly ShaderTagId _shaderTag = new("UniversalForward");

        public MaskPass(Settings settings)
        {
            _settings = settings;
            _whiteMat = new Material(Shader.Find("Unlit/Color"));
            _whiteMat.color = Color.white;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _maskRT, desc, name: "_StencilMaskTex");

            // 카메라의 깊이 버퍼를 함께 사용 → 가려진 부분은 깊이 테스트에서 탈락
            var depthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            ConfigureTarget(_maskRT, depthTarget);
            ConfigureClear(ClearFlag.Color, Color.clear); // 깊이는 클리어하지 않음 (기존 깊이 유지)
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawSettings = CreateDrawingSettings(_shaderTag, ref renderingData, SortingCriteria.CommonOpaque);
            drawSettings.overrideMaterial = _whiteMat;

            var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _settings.outlineLayerMask);

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalTexture("_StencilMaskTex", _maskRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _maskRT?.Release();
            if (_whiteMat != null) Object.DestroyImmediate(_whiteMat);
        }
    }

    // =============================================
    // Pass 2: 마스크 경계에 아웃라인 그리기
    // =============================================
    class OutlinePass : ScriptableRenderPass
    {
        private Settings _settings;
        private RTHandle _tempRT;

        public OutlinePass(Settings settings)
        {
            _settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_TempOutline");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_tempRT?.rt == null) return;

            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (source?.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("OutlineDraw");

            _settings.outlineMaterial.SetColor("_OutlineColor", _settings.outlineColor);
            _settings.outlineMaterial.SetFloat("_OutlineWidth", _settings.outlineWidth);

            // .nameID로 명시적 변환 → assertion 회피
            cmd.Blit(source.nameID, _tempRT.nameID, _settings.outlineMaterial);
            cmd.Blit(_tempRT.nameID, source.nameID);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempRT?.Release();
        }
    }
}