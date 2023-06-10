Shader "Klay/Unlit/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
                 _OutlineCol("OutlineCol", Color) = (1,0,0,1)  
         _OutlineFactor("OutlineFactor", Range(0,10)) = 0.1  
    }
    
    SubShader
    {
        cull front
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            float _OutlineFactor;  
            float4 _OutlineCol;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                v.vertex.xyz *= _OutlineFactor;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineCol;
            }
            ENDCG
        }
    }
}