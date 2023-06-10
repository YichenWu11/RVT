using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RendererPassTest : ScriptableRenderPass
{
    private readonly Material m_Mat = new(Shader.Find("Hidden/URPBaseTest"));

    // Profiling上显示
    private readonly ProfilingSampler m_ProfilingSampler = new("URPTest");
    private Color m_Color;

    // 当前阶段渲染的颜色RT
    private RenderTargetIdentifier m_Source;

    // 辅助RT
    private RenderTargetHandle m_TemporaryColorTexture;

    public RendererPassTest()
    {
        // 在哪个阶段插入渲染
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        // 初始化辅助RT名字
        m_TemporaryColorTexture.Init("URPBaseTest");
    }

    // RT的Filter
    public FilterMode filterMode { get; set; }

    public void Setup(RenderTargetIdentifier source, Color color)
    {
        m_Source = source;
        m_Color = color;
        m_Mat.SetColor("_Color", m_Color);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        // using的做法就是可以在FrameDebug上看到里面的所有渲染
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            // 创建一张RT
            var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_TemporaryColorTexture.id, opaqueDesc, filterMode);

            // 将当前帧的颜色RT用自己的着色器渲处理然后输出到创建的贴图上
            cmd.Blit(m_Source, m_TemporaryColorTexture.Identifier(), m_Mat);
            // 将处理后的RT重新渲染到当前帧的颜色RT上
            cmd.Blit(m_TemporaryColorTexture.Identifier(), m_Source);
        }

        // 执行
        context.ExecuteCommandBuffer(cmd);

        // 回收
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
        //销毁创建的RT
        cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
    }
}

public class RendererFeatureTest : ScriptableRendererFeature
{
    //会显示在资产面板上
    public Color m_Color = Color.red;

    private RendererPassTest m_Pass;

    //feature被创建时调用
    public override void Create()
    {
        m_Pass = new RendererPassTest();
    }

    //每一帧都会被调用
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //将当前渲染的颜色RT传到Pass中
        m_Pass.Setup(renderer.cameraColorTarget, m_Color);
        //将这个pass添加到渲染队列
        renderer.EnqueuePass(m_Pass);
    }
}