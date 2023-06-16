#ifndef VIRTUAL_TEXTURE_FEEDBACK_INCLUDED
#define VIRTUAL_TEXTURE_FEEDBACK_INCLUDED

#include "VT.cginc"
#include "UnityInstancing.cginc"

#define UNITY_INSTANCING_ENABLED

sampler2D _MainTex;
float4 _MainTex_TexelSize;

UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)
UNITY_INSTANCING_BUFFER_END(Terrain)

#ifdef UNITY_INSTANCING_ENABLED
TEXTURE2D(_TerrainHeightmapTexture);
SAMPLER(sampler_TerrainNormalmapTexture);
float4 _TerrainHeightmapRecipSize;
float4 _TerrainHeightmapScale;
#endif

struct feedback_v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct appdata_feedback
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

feedback_v2f VTVertFeedback(appdata_feedback v)
{
    feedback_v2f o;
    UNITY_SETUP_INSTANCE_ID(v);

    #ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = v.vertex.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    v.vertex.xz = sampleCoords * _TerrainHeightmapScale.xz;
    v.vertex.y = height * _TerrainHeightmapScale.y;

    v.texcoord = sampleCoords * _TerrainHeightmapRecipSize.zw;
    #endif

    VertexPositionInputs Attributes = GetVertexPositionInputs(v.vertex.xyz);

    o.pos = Attributes.positionCS;
    float2 posW = Attributes.positionWS.xz;
    // o.uv = (posW - _VTRealRect.xy) / _VTRealRect.zw;
    o.uv = v.texcoord;

    return o;
}

float4 VTFragFeedback(feedback_v2f i) : SV_Target
{
    float2 page = floor(i.uv * _VTFeedbackParam.x); // _VTFeedbackParam.x : PageTable TableSize

    float2 uv = i.uv * _VTFeedbackParam.y;
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    int mip = clamp(int(0.5 * log2(max(dot(dx, dx), dot(dy, dy))) + 0.5 + _VTFeedbackParam.w), 0, _VTFeedbackParam.z);

    return float4(page / 255.0f, mip / 255.0f, 1.0f);
    // return float4(float2(1, 2) / 255.0f, 3 / 255.0f, 1.0f);
}

/*
    _MainTex_TexelSize
        x : 1.0 / width
        y : 1.0 / height
        z : width
        w : height
 */

float4 GetMaxFeedback(float2 uv, int count)
{
    float4 color = float4(1, 1, 1, 1);
    for (int y = 0; y < count; y++)
    {
        for (int x = 0; x < count; x++)
        {
            float4 color1 = tex2D(_MainTex, uv + float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y));
            // 取 mipmapLevel 最小的 pixel
            // step(y, x) -> (x >= y) ? 1 : 0
            color = lerp(color, color1, step(color1.z, color.z)); // z : (mipmapLevel / 255.0f)
        }
    }
    return color;
}

#endif
