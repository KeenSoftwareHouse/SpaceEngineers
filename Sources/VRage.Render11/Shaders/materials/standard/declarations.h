struct MaterialConstants
{
	float4 key_color;
};

struct MaterialVertexPayload
{
#ifndef DEPTH_ONLY	
	float2 texcoord0 	 : TEXCOORD0;
	float3 local_forward : TEXCOORD1;
	float3 local_up		 : TEXCOORD2;
	float3 normal		 : NORMAL;
	float4 tangent		 : TANGENT;
#endif
};

Texture2D<float4> ColorMetalTexture : register( t0 );
Texture2D<float4> NormalGlossTexture : register( t1 );
Texture2D<float4> AmbientOcclusionTexture : register( t2 );
Texture2D<float> AlphamaskTexture : register( t3 );
