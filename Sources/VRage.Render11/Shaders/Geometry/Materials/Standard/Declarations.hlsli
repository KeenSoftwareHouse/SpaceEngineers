struct MaterialConstants
{
	float4 key_color;
};

struct MaterialVertexPayload
{
#ifndef DEPTH_ONLY	
	float2 texcoord0 	 : TEXCOORD0;
	float4 texIndices    : TEXCOORD1;
	float3 local_forward : TEXCOORD2;
	float3 local_up		 : TEXCOORD3;
	float3 normal		 : NORMAL;
	float4 tangent		 : TANGENT;
#endif
};

#ifdef USE_TEXTURE_INDICES
	Texture2DArray<float4> ColorMetalTexture : register(t0);
	Texture2DArray<float4> NormalGlossTexture : register(t1);
	Texture2DArray<float4> ExtensionsTexture : register(t2);
	Texture2DArray<float> AlphamaskTexture : register(t3);
#else
	Texture2D<float4> ColorMetalTexture : register( t0 );
	Texture2D<float4> NormalGlossTexture : register( t1 );
	Texture2D<float4> ExtensionsTexture : register(t2);
	Texture2D<float> AlphamaskTexture : register( t3 );
#endif

