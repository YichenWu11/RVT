using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class FeedbackRenderer : MonoBehaviour
{
    private static readonly int VTFeedbackParam = Shader.PropertyToID("_VTFeedbackParam");

    [SerializeField] private int mipmapBias;

    // Feedback RT 缩放
    private readonly float _scaleFactor = 1.0f / 16.0f;

    public Camera FeedbackCamera { get; private set; }

    // Feedback RT
    public RenderTexture TargetTexture { get; private set; }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        FollowMainCamera();
    }

    private void Init()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        FeedbackCamera = GetComponent<Camera>();
        if (FeedbackCamera == null) FeedbackCamera = gameObject.AddComponent<Camera>();
        FeedbackCamera.enabled = false;

        var width = (int)(mainCamera.pixelWidth * _scaleFactor);
        var height = (int)(mainCamera.pixelHeight * _scaleFactor);
        if (TargetTexture == null || TargetTexture.width != width || TargetTexture.height != height)
        {
            TargetTexture = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                useMipMap = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            FeedbackCamera.targetTexture = TargetTexture;
        }

        // x: PageTable Size
        // y: Virtual Texture Size
        // z: Max MipMap Level
        var tileTexture = GetComponent<TiledTexture>();
        var virtualTable = GetComponent<PageTable>();
        Shader.SetGlobalVector(
            VTFeedbackParam,
            new Vector4(
                virtualTable.TableSize,
                virtualTable.TableSize * tileTexture.TileSize * _scaleFactor,
                virtualTable.MaxMipLevel - 1,
                mipmapBias));
    }

    private void FollowMainCamera()
    {
        var mainCamera = Camera.main;
        var fbTransform = FeedbackCamera.transform;
        var mcTransform = mainCamera.transform;
        fbTransform.position = mcTransform.position;
        fbTransform.rotation = mcTransform.rotation;
        FeedbackCamera.projectionMatrix = mainCamera.projectionMatrix;
    }
}