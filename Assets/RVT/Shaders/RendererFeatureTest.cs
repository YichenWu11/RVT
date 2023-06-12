using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RendererPassTest : ScriptableRenderPass
{
    private static readonly int RenderFeatureTestColor = Shader.PropertyToID("_RenderFeatureTest_Color");
    private static readonly int TerrainWidth = Shader.PropertyToID("_Terrain_Width");
    private static readonly int TerrainLength = Shader.PropertyToID("_Terrain_Length");
    private static readonly int VTFeedbackParam = Shader.PropertyToID("_VTFeedbackParam");

    private readonly Material _mat = new(Shader.Find("Hidden/URPBaseTest"));
    private readonly ProfilingSampler _profilingSampler = new("URPTest");
    private readonly RenderTargetHandle _tempColorTexture;
    private Color _color;

    private RenderTexture _depth;
    private RenderTexture _gt;

    private RenderTargetIdentifier _source;

    public RendererPassTest()
    {
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        // renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        _tempColorTexture.Init("URPBaseTest");
        _depth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth,
            RenderTextureReadWrite.Linear);
        _gt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
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
            // var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            // opaqueDesc.depthBufferBits = 0;
            // cmd.GetTemporaryRT(_tempColorTexture.id, opaqueDesc, filterMode);
            Shader.SetGlobalColor(RenderFeatureTestColor, _color);
            Shader.SetGlobalFloat(TerrainWidth, 1024.0f);
            Shader.SetGlobalFloat(TerrainLength, 1024.0f);

            var pageSize = 64.0f;
            var virtualTextureSize = 2056.0f;
            var maxMipLevel = 3.0f;

            var feedbackParam = new Vector4(pageSize, virtualTextureSize, maxMipLevel, 0.0f);
            Shader.SetGlobalVector(VTFeedbackParam, feedbackParam);

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