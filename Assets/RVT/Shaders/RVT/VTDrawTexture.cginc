#ifndef VIRTUAL_DRAW_TEXTURE_INCLUDED
#define VIRTUAL_DRAW_TEXTURE_INCLUDED

sampler2D _Diffuse1;
sampler2D _Diffuse2;
sampler2D _Diffuse3;
sampler2D _Diffuse4;
sampler2D _Normal1;
sampler2D _Normal2;
sampler2D _Normal3;
sampler2D _Normal4;

float4x4 _ImageMVP;

sampler2D _Blend;
float4 _BlendTile;

float4 _TileOffset1;
float4 _TileOffset2;
float4 _TileOffset3;
float4 _TileOffset4;


struct pixelOutput_drawTex
{
    float4 col0 : COLOR0;
    float4 col1 : COLOR1;
};

struct v2f_drawTex
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f_drawTex vert(appdata_img v)
{
    v2f_drawTex o;
    o.pos = mul(_ImageMVP, v.vertex);
    o.uv = v.texcoord;

    return o;
}

pixelOutput_drawTex frag(v2f_drawTex i) : SV_Target
{
    float4 blend = tex2D(_Blend, i.uv * _BlendTile.xy + _BlendTile.zw);

    // float2 transUv = i.uv * _TileOffset1.xy + _TileOffset1.zw;
    // float4 diffuse1 = tex2Dlod(_Diffuse1, float4(transUv, 0, 0));
    // float4 normal1 = tex2Dlod(_Normal1, float4(transUv, 0, 0));
    //
    // transUv = i.uv * _TileOffset2.xy + _TileOffset2.zw;
    // float4 diffuse2 = tex2Dlod(_Diffuse2, float4(transUv, 0, 0));
    // float4 normal2 = tex2Dlod(_Normal2, float4(transUv, 0, 0));
    //
    // transUv = i.uv * _TileOffset3.xy + _TileOffset3.zw;
    // float4 diffuse3 = tex2Dlod(_Diffuse3, float4(transUv, 0, 0));
    // float4 normal3 = tex2Dlod(_Normal3, float4(transUv, 0, 0));
    //
    // transUv = i.uv * _TileOffset4.xy + _TileOffset4.zw;
    // float4 diffuse4 = tex2Dlod(_Diffuse4, float4(transUv, 0, 0));
    // float4 normal4 = tex2Dlod(_Normal4, float4(transUv, 0, 0));

    float2 transUv = i.uv * _TileOffset1.xy + _TileOffset1.zw;
    float4 diffuse1 = tex2D(_Diffuse1, transUv);
    float4 normal1 = tex2D(_Normal1, transUv);

    transUv = i.uv * _TileOffset2.xy + _TileOffset2.zw;
    float4 diffuse2 = tex2D(_Diffuse2, transUv);
    float4 normal2 = tex2D(_Normal2, transUv);

    transUv = i.uv * _TileOffset3.xy + _TileOffset3.zw;
    float4 diffuse3 = tex2D(_Diffuse3, transUv);
    float4 normal3 = tex2D(_Normal3, transUv);

    transUv = i.uv * _TileOffset4.xy + _TileOffset4.zw;
    float4 diffuse4 = tex2D(_Diffuse4, transUv);
    float4 normal4 = tex2D(_Normal4, transUv);

    pixelOutput_drawTex o;
    // o.col0 = Diffuse2;
    o.col0 = blend.r * diffuse1 + blend.g * diffuse2 + blend.b * diffuse3 + blend.a * diffuse4;
    o.col1 = blend.r * normal1 + blend.g * normal2 + blend.b * normal3 + blend.a * normal4;
    return o;
}

#endif
