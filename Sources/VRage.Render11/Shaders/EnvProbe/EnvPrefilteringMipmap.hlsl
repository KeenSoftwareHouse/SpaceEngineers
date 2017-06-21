#include "EnvPrefiltering.hlsli"

[numthreads(8, 8, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	UAOutput[uint3(texel, 0)] = (
	 	PrevMipmap[uint3(texel * 2, 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(1, 0), 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(0, 1), 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(1, 1), 0)]) * 0.25;
}
