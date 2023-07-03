using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class TextureCompressUtil
{
    public static Texture2D CompressRT2Tex2D(ComputeShader shader, RenderTexture rt)
    {
        var destRect = new int[4] { 0, 0, rt.width, rt.height };
        var compressedRT = new RenderTexture(rt.width / 4, rt.height / 4, 0)
        {
            graphicsFormat = GraphicsFormat.R32G32B32A32_UInt,
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        compressedRT.Create();

        var kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Result", compressedRT);
        shader.SetTexture(kernelHandle, "RT0", rt);
        shader.SetInts("DestRect", destRect);
        shader.Dispatch(kernelHandle, (rt.width / 4 + 7) / 8, (rt.height / 4 + 7) / 8, 1);

        var compressedTex2D =
            new Texture2D(rt.width, rt.height, GraphicsFormat.RGBA_DXT5_UNorm, TextureCreationFlags.None);
        Graphics.CopyTexture(
            compressedRT,
            0, 0, 0, 0,
            rt.width / 4, rt.height / 4,
            compressedTex2D, 0, 0, 0, 0);

        compressedRT.Release();

        return compressedTex2D;
    }

    public static RenderTexture CompressRT2RT(ComputeShader shader, RenderTexture rt)
    {
        var destRect = new int[4] { 0, 0, rt.width, rt.height };
        var compressedRT = new RenderTexture(rt.width / 4, rt.height / 4, 0)
        {
            graphicsFormat = GraphicsFormat.R32G32B32A32_UInt,
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        compressedRT.Create();

        var kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Result", compressedRT);
        shader.SetTexture(kernelHandle, "RT0", rt);
        shader.SetInts("DestRect", destRect);
        shader.Dispatch(kernelHandle, (rt.width / 4 + 7) / 8, (rt.height / 4 + 7) / 8, 1);

        return compressedRT;
    }
}