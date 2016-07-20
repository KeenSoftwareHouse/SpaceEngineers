// @defineMandatory NUMTHREADS 8

#include <tonemapping.h>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	float4 result = 0;
	for(int i=-6; i<6; i++) {
		float4 sample = GenericSource[texel + float2(0, i)];

		result += sample * GaussianWeight(i, 2);
	}

	Destination[texel] = result;
}
