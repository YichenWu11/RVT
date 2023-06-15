using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// 负责将预渲染 RT从 GPU 回读到 CPU
public class FeedbackReader : MonoBehaviour
{
    // 回读目标缩放比例
    [SerializeField] private ScaleFactor m_ReadbackScale;

    // 缩放着色器, 找到区域中 mipmap 等级最小的像素作为最终像素，其余像素抛弃
    [SerializeField] private Shader m_DownScaleShader;

    // 用于在编辑器中显示贴图 mipmap 等级
    [SerializeField] private Shader m_DebugShader;

    // 调试材质 用于在编辑器中显示贴图 mipmap 等级
    private Material m_DebugMaterial;

    // 缩放材质
    private Material m_DownScaleMaterial;

    // 缩放材质使用的 Pass
    private int m_DownScaleMaterialPass;

    // 缩小后的 RT
    private RenderTexture m_DownScaleTexture;

    // 处理中的回读请求
    private AsyncGPUReadbackRequest m_ReadbackRequest;

    // 回读到 CPU 的贴图
    private Texture2D m_ReadbackTexture;

    // 调试用的 RenderTexture (用于显示 mipmap 等级)
    public RenderTexture DebugTexture { get; private set; }

    public bool CanRead => m_ReadbackRequest.done || m_ReadbackRequest.hasError;

    private void Start()
    {
        if (m_ReadbackScale != ScaleFactor.One)
        {
            m_DownScaleMaterial = new Material(m_DownScaleShader);

            switch (m_ReadbackScale)
            {
                case ScaleFactor.Half:
                    m_DownScaleMaterialPass = 0;
                    break;
                case ScaleFactor.Quarter:
                    m_DownScaleMaterialPass = 1;
                    break;
                case ScaleFactor.Eighth:
                    m_DownScaleMaterialPass = 2;
                    break;
            }
        }
    }

    // 回读完成的事件回调
    public event Action<Texture2D> OnFeedbackReadComplete;

    // 发起回读请求
    public void ReadbackRequest(RenderTexture texture, bool forceWait = false)
    {
        if (m_ReadbackRequest is { done: false, hasError: false })
            return;

        // 缩放后的尺寸
        var width = (int)(texture.width * m_ReadbackScale.ToFloat());
        var height = (int)(texture.height * m_ReadbackScale.ToFloat());

        // 先进行缩放
        if (m_ReadbackScale != ScaleFactor.One)
        {
            if (m_DownScaleTexture == null || m_DownScaleTexture.width != width || m_DownScaleTexture.height != height)
                m_DownScaleTexture = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm);

            Graphics.Blit(texture, m_DownScaleTexture, m_DownScaleMaterial, m_DownScaleMaterialPass);
            texture = m_DownScaleTexture;
        }

        // 贴图尺寸检测
        if (m_ReadbackTexture == null || m_ReadbackTexture.width != width || m_ReadbackTexture.height != height)
        {
            m_ReadbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

#if UNITY_EDITOR
            DebugTexture = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
#endif
        }

        // 异步回读请求
        m_ReadbackRequest = AsyncGPUReadback.Request(texture);

        if (forceWait) m_ReadbackRequest.WaitForCompletion();
    }

    public void UpdateRequest()
    {
        if (m_ReadbackRequest is { done: true, hasError: false })
        {
            var colors = m_ReadbackRequest.GetData<Color32>();
            m_ReadbackTexture.GetRawTextureData<Color32>().CopyFrom(colors);

            // 把在 CPU 端的更改同步到 GPU 端
            m_ReadbackTexture.Apply(false);
            OnFeedbackReadComplete?.Invoke(m_ReadbackTexture);
            UpdateDebugTexture();
        }
    }

    private void UpdateDebugTexture()
    {
#if UNITY_EDITOR
        if (m_ReadbackTexture == null || m_DebugShader == null)
            return;

        if (m_DebugMaterial == null)
            m_DebugMaterial = new Material(m_DebugShader);

        Graphics.Blit(m_ReadbackTexture, DebugTexture, m_DebugMaterial);
#endif
    }
}