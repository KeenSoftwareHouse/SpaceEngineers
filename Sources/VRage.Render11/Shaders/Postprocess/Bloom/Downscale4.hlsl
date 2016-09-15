// @defineMandatory NUMTHREADS 8

#include "Defines.hlsli"

// this approach is chosen because of stereo rendering of both eyes
cbuffer BlurHParams :register(b1)
{
	float2 resolution;
}

float4 Sample(int2 texel)
{
    float2 fTexel = (float2(texel) + 1) / resolution;
    return Source.SampleLevel(BilinearSampler, fTexel, 0);
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) 
{
	int2 texel = dispatchThreadID.xy;

	const int2 offsets[4] = 
    {
        uint2(1, 1), uint2(-1, -1),
        uint2(-1, 1), uint2(1, -1),
    };

    // manual interpolation vs sending texcoords
    float4 result = 0;
    [unroll]
    for(uint i=0; i<4; i++) 
    {
    	result += Sample(texel * 4 + offsets[i]);
    }
    Destination[texel] = result / 4;
}
