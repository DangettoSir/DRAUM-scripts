using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Рендерит VolumetricFog ПОСЛЕ постобработки (виньетка, цветокоррекция уже применены).
/// В Renderer добавь эту фичу, назначь материал VolumetricFog. Старый Full Screen Pass для тумана можно отключить.
/// </summary>
public class VolumetricFogRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Material _passMaterial;
    [Tooltip("AfterRenderingPostProcessing = туман поверх постобработки")]
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private VolumetricFogPass _pass;

    public override void Create()
    {
        _pass = new VolumetricFogPass(_renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_passMaterial == null)
        {
            Debug.LogWarning("[VolumetricFog] Pass Material не назначен.");
            return;
        }
        _pass.Setup(_passMaterial);
        renderer.EnqueuePass(_pass);
    }

    private class VolumetricFogPass : ScriptableRenderPass
    {
        private const string k_RenderTag = "VolumetricFog";
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");

        private Material _material;
        private RTHandle _cameraColorHandle;

        public VolumetricFogPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(Material mat) => _material = mat;

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _cameraColorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _cameraColorHandle == null) return;

            var cameraData = renderingData.cameraData;
            cameraData.camera.depthTextureMode |= DepthTextureMode.Depth;

            var cmd = CommandBufferPool.Get(k_RenderTag);
            cmd.SetGlobalTexture(BlitTextureId, _cameraColorHandle);
            Blitter.BlitCameraTexture(cmd, _cameraColorHandle, _cameraColorHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, _material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
