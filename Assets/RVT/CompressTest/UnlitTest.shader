Shader "Klay/UnlitTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _Compressed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 decodeDXT5(float4 color)
            {
                // 解压缩纹理数据
                float3 c0 = color.rgb;
                float3 c1 = color.a * float3(1, 1, 1);
                float3 c2 = (2 * c0.rgb + c1.rgb) / 3;
                float3 c3 = (c0.rgb + 2 * c1.rgb) / 3;

                // 返回解压缩后的颜色值
                return float4(c2.rgb, c3.r);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_Compressed, i.uv);
                // return decodeDXT5(col);
                return col;
            }
            ENDCG
        }
    }
}
