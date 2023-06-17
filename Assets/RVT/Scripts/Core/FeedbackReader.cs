using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// 负责将预渲染 RT从 GPU 回读到 CPU
public class FeedbackReader : MonoBehaviour
{
    // 回读目标缩放比例
    [SerializeField] private ScaleFactor readbackScale;

    // 缩放着色器, 找到区域中 mipmap 等级最小的像素作为最终像素，其余像素抛弃
    [SerializeField] private Shader downScaleShader;

    // 用于在编辑器中显示贴图 mipmap 等级
    [SerializeField] private Shader debugShader;

    // 调试材质 用于在编辑器中显示贴图 mipmap 等级
    private Material _debugMaterial;

    // 缩放材质
    private Material _downScaleMaterial;

    // 缩放材质使用的 Pass
    private int _downScaleMaterialPass;

    // 缩小后的 RT
    private RenderTexture _downScaleTexture;

    // 处理中的回读请求
    private AsyncGPUReadbackRequest _readbackRequest;

    // 回读到 CPU 的贴图
    private Texture2D _readbackTexture;

    // 调试用的 RenderTexture (用于显示 mipmap 等级)
    public RenderTexture DebugTexture { get; private set; }

    public bool CanRead => _readbackRequest.done || _readbackRequest.hasError;

    private void Start()
    {
        if (readbackScale != ScaleFactor.One)
        {
            _downScaleMaterial = new Material(downScaleShader);

            switch (readbackScale)
            {
                case ScaleFactor.Half:
                    _downScaleMaterialPass = 0;
                    break;
                case ScaleFactor.Quarter:
                    _downScaleMaterialPass = 1;
                    break;
                case ScaleFactor.Eighth:
                    _downScaleMaterialPass = 2;
                    break;
            }
        }
    }

    // 回读完成的事件
    public event Action<Texture2D> OnFeedbackReadComplete;

    // 发起回读请求
    public void ReadbackRequest(RenderTexture texture, bool forceWait = false)
    {
        if (_readbackRequest is { done: false, hasError: false })
            return;

        // 缩放后的尺寸
        var width = (int)(texture.width * readbackScale.ToFloat());
        var height = (int)(texture.height * readbackScale.ToFloat());

        // 缩放
        if (readbackScale != ScaleFactor.One)
        {
            if (_downScaleTexture == null || _downScaleTexture.width != width || _downScaleTexture.height != height)
                _downScaleTexture = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm);

            Graphics.Blit(texture, _downScaleTexture, _downScaleMaterial, _downScaleMaterialPass);
            texture = _downScaleTexture;
        }

        if (_readbackTexture == null || _readbackTexture.width != width || _readbackTexture.height != height)
        {
            _readbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
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
        _readbackRequest = AsyncGPUReadback.Request(texture);

        if (forceWait) _readbackRequest.WaitForCompletion();
    }

    public void UpdateRequest()
    {
        if (_readbackRequest is { done: true, hasError: false })
        {
            var colors = _readbackRequest.GetData<Color32>();
            _readbackTexture.GetRawTextureData<Color32>().CopyFrom(colors);

            // 把在 CPU 端的更改同步到 GPU 端
            _readbackTexture.Apply(false);
            OnFeedbackReadComplete?.Invoke(_readbackTexture);
            UpdateDebugTexture();
        }
    }

    private void UpdateDebugTexture()
    {
#if UNITY_EDITOR
        if (_readbackTexture == null || debugShader == null)
            return;

        if (_debugMaterial == null)
            _debugMaterial = new Material(debugShader);

        Graphics.Blit(_readbackTexture, DebugTexture, _debugMaterial);
#endif
    }
}