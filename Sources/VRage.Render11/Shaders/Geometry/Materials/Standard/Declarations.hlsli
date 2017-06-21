struct MaterialConstants
{
};

struct MaterialVertexPayload
{
#ifndef DEPTH_ONLY	
	float2 texcoord0 	 : TEXCOORD0;
#ifdef USE_TEXTURE_INDICES
	float4 texIndices    : TEXCOORD1;
#endif
	float3 normal		 : NORMAL;
	float4 tangent		 : TANGENT;
#endif
};

#ifdef USE_TEXTURE_INDICES
	Texture2DArray<float4> ColorMetalArrayTexture : register(t0);
	Texture2DArray<float4> NormalGlossArrayTexture : register(t1);
	Texture2DArray<float4> ExtensionsArrayTexture : register(t2);
	Texture2DArray<float> AlphamaskArrayTexture : register(t3);
#else
	Texture2D<float4> ColorMetalTexture : register( t0 );
	Texture2D<float4> NormalGlossTexture : register( t1 );
	Texture2D<float4> ExtensionsTexture : register(t2);
	Texture2D<float> AlphamaskTexture : register( t3 );
#endif
