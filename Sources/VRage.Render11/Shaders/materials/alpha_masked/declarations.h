#define CUSTOM_DEPTH

struct MaterialConstants
{
	float4 key_color;
};

struct MaterialVertexPayload
{
#if !defined(DEPTH_ONLY) || defined(ALPHA_MASKED)
	float2 texcoord0 	: TEXCOORD0;
	float3 cDir : TEXCOORD1;
	int3 view_indices : TEXCOORD2;
	float3 view_blends : TEXCOORD3;
	int3 view_indices_light : TEXCOORD4;
	float3 view_blends_light : TEXCOORD5;
	float3 cPos : TEXCOORD6;
	float3 lDir : TEXCOORD8;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
#endif
};

#ifdef ALPHA_MASK_ARRAY
Texture2DArray AlphaMaskArrayTexture : register ( t0 );
#else
Texture2D<float4> ColorMetalTexture : register( t0 );
#endif
Texture2D<float4> NormalGlossTexture : register( t1 );
Texture2D<float4> AmbientOcclusionTexture : register( t2 );
Texture2D<float4> AlphamaskTexture : register( t3 );
