struct MaterialConstants
{
	float4 key_color;
};

struct MaterialVertexPayload
{
#if !defined(DEPTH_ONLY) || defined(ALPHA_MASKED)
	float2 texcoord0 	: TEXCOORD0;
#endif
#ifndef DEPTH_ONLY
	float3 normal 		: NORMAL;
	float4 tangent		: TANGENT;
#endif
};

Texture2D<float4> ColorMetalTexture : register( t0 );
Texture2D<float4> NormalGlossTexture : register( t1 );
Texture2D<float4> AmbientOcclusionTexture : register( t2 );
Texture2D<float> AlphamaskTexture : register( t3 );
