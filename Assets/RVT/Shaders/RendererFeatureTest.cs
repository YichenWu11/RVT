using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RendererPassTest : ScriptableRenderPass
{
    private readonly Material _mat = new(Shader.Find("Hidden/URPBaseTest"));
    private readonly ProfilingSampler _profilingSampler = new("URPTest");
    private readonly RenderTargetHandle _tempColorTexture;
    private Color _color;

    public RenderTexture _dest;
    private RenderTargetIdentifier _source;

    public RendererPassTest()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        // renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        _tempColorTexture.Init("URPBaseTest");
    }

    public FilterMode filterMode { get; set; }

    public void Setup(RenderTargetIdentifier source, Color color)
    {
        _source = source;
        _color = color;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _profilingSampler))
        {
            var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(_tempColorTexture.id, opaqueDesc, filterMode);

            // Blit(cmd, _source, _tempColorTexture.Identifier(), _mat);
            // Blit(cmd, _tempColorTexture.Identifier(), _source);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        base.OnCameraCleanup(cmd);
        cmd.ReleaseTemporaryRT(_tempColorTexture.id);
    }
}

public class RendererFeatureTest : ScriptableRendererFeature
{
    public Color color = Color.red;

    private RendererPassTest pass;

    public override void Create()
    {
        pass = new RendererPassTest();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.Setup(renderer.cameraColorTarget, color);
        renderer.EnqueuePass(pass);
    }
}