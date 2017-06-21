#ifndef ENV_PREFILTERING__
#define ENV_PREFILTERING__

#include <Common.hlsli>
#include <Lighting/Brdf.hlsli>

TextureCube<float4> ProbeTex : register( t0 );
Texture2DArray<float4> PrevMipmap : register( t0 );
RWTexture2DArray<float4> UAOutput : register( u0 );

cbuffer Constants : register( b1 )
{
	uint SamplesNum; // power of 2
	uint EnvProbeRes;
	uint MipResolution;
	uint FaceId;
	float Gloss_Blend;
}

#endif
