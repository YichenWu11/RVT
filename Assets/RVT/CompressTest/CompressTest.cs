using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CompressTest : MonoBehaviour
{
    [HideInInspector] public RenderTexture unCompressed;
    [HideInInspector] public RenderTexture compressed;
    [HideInInspector] public Texture2D compressed2D;

    [SerializeField] private Material mat0;
    [SerializeField] private Material mat1;

    public ComputeShader shader;
    private int _kernelHandle;
    private int[] _destRect;
    private static readonly int Compressed = Shader.PropertyToID("_Compressed");

    private void Awake()
    {
        _destRect = new int[4] { 0, 0, 256, 256 };

        unCompressed = new RenderTexture(256, 256, 0)
        {
            filterMode = FilterMode.Point,
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
        };
        unCompressed.Create();

        compressed = new RenderTexture(64, 64, 0)
        {
            graphicsFormat = GraphicsFormat.R32G32B32A32_UInt,
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        compressed.Create();

        compressed2D = new Texture2D(256, 256, GraphicsFormat.RGBA_DXT5_UNorm, TextureCreationFlags.None);

        Graphics.Blit(null, unCompressed, mat0);
    }

    private void Start()
    {
        _kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(_kernelHandle, "Result", compressed);
        shader.SetTexture(_kernelHandle, "RenderTexture0", unCompressed);
        shader.SetInts("DestRect", _destRect);
        shader.Dispatch(_kernelHandle, (256 / 4 + 7) / 8, (256 / 4 + 7) / 8, 1);

        // Shader.SetGlobalTexture(Compressed, compressed);
        Graphics.CopyTexture(compressed, 0, 0, 0, 0, 64, 64, compressed2D, 0, 0, 0, 0);
        Shader.SetGlobalTexture(Compressed, compressed2D);
    }
}