Shader "Hidden/URPBaseTest"
{
	Properties
	{
		_MainTex("ScreenTexture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
 
		Pass
		{
			 HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 
			#pragma vertex vert
			#pragma fragment frag
 
			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};
 
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _RenderFeatureTest_Color;
 
			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = vertexInput.positionCS; // clip space
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
 
			half4 frag(Varyings i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				return lerp(col, _RenderFeatureTest_Color, 0.2);
				// return half4(i.uv, 0.0h, 1.0h);
			}
			ENDHLSL
		}
	}
}