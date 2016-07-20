// @defineMandatory NUMTHREADS 8

#include <tonemapping.h>

// this approach is chosen because of stereo rendering of both eyes
cbuffer BlurHParams :register(b1)
{
	int offsetX;
	int maxX;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {

	float4 result = 0;
	for(int i=-6; i<6; i++) 
	{
		int2 coord = dispatchThreadID.xy;
		coord.x = offsetX + clamp(coord.x + i, 0, maxX);
		float4 sample = GenericSource[coord];

		result += sample * GaussianWeight(i, 2);
	}

	int2 outTexel = dispatchThreadID.xy;
	outTexel.x += offsetX;
	Destination[outTexel] = result;
}
