Shader "Klay/VT/DebugMipmap"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex VTVert
			#pragma fragment frag

			#include "VT.cginc"
			
			sampler2D _MainTex;

			float4 frag(VTV2f i) : SV_Target
			{
				return VTDebugMipmapLevel(_MainTex, i.uv);
			}
			ENDHLSL
		}
	}
}
