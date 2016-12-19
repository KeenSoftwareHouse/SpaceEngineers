RWTexture2D<float2> Output0 : register( u0 );

#define ATMOSPHERE_PRECOMPUTE
#include <Common.hlsli>
#include "AtmosphereCommon.hlsli"

[numthreads(8, 8, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) 
{
	float2 uv = (float2)(dispatchThreadID.xy) / (float2(512, 128) - 1);

	float3 P1, P2;
	GetPointsFromUv(uv, P1, P2);
	float2 opticalDepth = ComputeOpticalDepth(P1, P2, 100);
	Output0[dispatchThreadID.xy] = opticalDepth;
}