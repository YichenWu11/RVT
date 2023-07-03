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
        _destRect = new int[4] { 0, 0, 1024, 1024 };

        // unCompressed = new RenderTexture(5120, 5120, 0)
        // {
        //     filterMode = FilterMode.Point,
        //     graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
        // };
        // unCompressed.Create();

        // compressed = new RenderTexture(256, 256, 0)
        // {
        //     graphicsFormat = GraphicsFormat.R32G32B32A32_UInt,
        //     enableRandomWrite = true,
        //     filterMode = FilterMode.Point
        // };
        // compressed.Create();

        compressed2D = new Texture2D(5120, 5120, GraphicsFormat.RGBA_DXT5_UNorm, TextureCreationFlags.None);
        compressed2D.Apply();
        // Graphics.Blit(null, unCompressed, mat0);
    }

    private void Start()
    {
        // _kernelHandle = shader.FindKernel("CSMain");
        //
        // shader.SetTexture(_kernelHandle, "Result", compressed);
        // shader.SetTexture(_kernelHandle, "RT0", unCompressed);
        // shader.SetInts("DestRect", _destRect);
        // shader.Dispatch(_kernelHandle, (1024 / 4 + 7) / 8, (1024 / 4 + 7) / 8, 1);
        //
        // // Shader.SetGlobalTexture(Compressed, compressed);
        // // Graphics.CopyTexture(compressed, 0, 0, 0, 0, 256, 256, compressed2D, 0, 0, 0, 0);
        // // Shader.SetGlobalTexture(Compressed, compressed2D);
        //
        // compressed2D = TextureCompressUtil.CompressRT2Tex2D(shader, unCompressed);
        // Shader.SetGlobalTexture(Compressed, compressed2D);
    }
}