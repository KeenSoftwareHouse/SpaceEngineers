// @defineMandatory NUMTHREADS 8

#include <tonemapping.h>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	const uint2 offsets[4] = {
        uint2(0, 0), uint2(1, 0),
        uint2(1, 0), uint2(1, 1),
    };

    // manual interpolation vs sending texcoords
    float4 result = 0;
    [unroll]
    for(uint i=0; i<4; i++) {
    	result += GenericSource[texel * 2 + offsets[i]];
    }
	Destination[texel] = result / 4.f;
}
