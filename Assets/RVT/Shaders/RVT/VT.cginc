#ifndef VIRTUAL_TEXTURE_INCLUDED
#define VIRTUAL_TEXTURE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct VTAppdata
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct VTV2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

// x : page size
// y : virtual texture size
// z : max mipmap level
// w : mipmap level bias
float4 _VTFeedbackParam;

// x : table size
// y : 1 / table size
// z :  max mipmap level
float4 _VTPageParam;
sampler2D _VTLookupTex;

// x : 0
// y : tile size
// zw: region size * tile size 
float4 _VTTileParam;
sampler2D _VTDiffuse;
sampler2D _VTNormal;

float4 _VTRealRect;


VTV2f VTVertFromPos(VTAppdata v)
{
    VTV2f o;

    o.pos = TransformObjectToHClip(v.vertex.xyz);
    float2 posW = TransformObjectToWorld(v.vertex).xz;
    o.uv = (posW - _VTRealRect.xy) / _VTRealRect.zw;

    return o;
}

VTV2f VTVert(VTAppdata v)
{
    VTV2f o;

    o.pos = TransformObjectToHClip(v.vertex.xyz);
    o.uv = v.texcoord;
    return o;
}

float2 VTTransferUV(float2 uv)
{
    float2 uvInt = uv - frac(uv * _VTPageParam.x) * _VTPageParam.y;
    float4 page = tex2D(_VTLookupTex, uvInt) * 255;
    float2 inPageOffset = frac(uv * exp2(_VTPageParam.z - page.b));
    return (page.rg * (_VTTileParam.y + _VTTileParam.x * 2) + inPageOffset * _VTTileParam.y + _VTTileParam.x) /
        _VTTileParam.zw;
}

float4 VTTex2DDiffuse(float2 uv)
{
    //return fixed4(uv, 0, 1);
    return tex2D(_VTDiffuse, uv);
}

float4 VTTex2D1(float2 uv)
{
    return tex2D(_VTNormal, uv);
}

float4 VTTex2D(float2 uv)
{
    return VTTex2DDiffuse(uv);
}

// FOR DEBUG
float4 VTGetMipmapLevelColor(float mip)
{
    const float4 colors[12] = {
        float4(1, 0, 0, 1),
        float4(0, 0, 1, 1),
        float4(1, 0.5f, 0, 1),
        float4(1, 0, 0.5f, 1),
        float4(0, 0.5f, 0.5f, 1),
        float4(0, 0.25f, 0.5f, 1),
        float4(0.25f, 0.5f, 0, 1),
        float4(0.5f, 0, 1, 1),
        float4(1, 0.25f, 0.5f, 1),
        float4(0.5f, 0.5f, 0.5f, 1),
        float4(0.25f, 0.25f, 0.25f, 1),
        float4(0.125f, 0.125f, 0.125f, 1)
    };
    return colors[clamp(mip, 0, 11)];
}


float4 VTDebugMipmapLevel(sampler2D tex, float2 uv) : SV_Target
{
    return VTGetMipmapLevelColor(tex2D(tex, uv).z * 255.0f);
}

#endif
