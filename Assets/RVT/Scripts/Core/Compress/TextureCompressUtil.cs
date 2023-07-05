using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class TextureCompressUtil
{
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