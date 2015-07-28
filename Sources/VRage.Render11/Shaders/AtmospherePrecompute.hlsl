cbuffer Constants : register( b1 )
{
	float 	RadiusGround;
	float 	RadiusAtmosphere;
	float2 	HeightScaleRayleighMie;
	float3 	BetaRayleighScattering;
	float 	RadiusLimit;
	float3 	BetaMieScattering;
}

RWTexture2D<float4> Output0 : register( u0 );
RWTexture3D<float4> Output3D_0 : register( u0 );
RWTexture3D<float4> Output3D_1 : register( u1 );

static const float BetaRatio = 0.9f;

Texture2D<float4> DensityLut : register( t0 );

#include <common.h>
#include <AtmosphereCommon.h>

[numthreads(8, 8, 1)]
void precomputeDensity(uint3 dispatchThreadID : SV_DispatchThreadID) {
	float2 uv = (float2)(dispatchThreadID.xy) / (float2(256, 64) - 1);
	
	Output0[dispatchThreadID.xy] = float4(PrecomputeNetDensityToAtmTopPS(uv), 0, 0);
}

[numthreads(8, 8, 1)]
void precomputeInscatter1(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint3 xyzCoord = dispatchThreadID;
	xyzCoord.z = xyzCoord.z % 32;

	float4 uvwq = float4(xyzCoord, dispatchThreadID.z / 32) / (PRECOMPUTED_SCTR_LUT_DIM - 1);

	Output3D_0[dispatchThreadID] = float4(PrecomputeSingleScattering(uvwq, 64), 1);
	//Output3D_1[dispatchThreadID] = float4(mie, 0);
}

