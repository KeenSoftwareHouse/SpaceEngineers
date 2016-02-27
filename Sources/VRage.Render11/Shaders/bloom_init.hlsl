// @defineMandatory NUMTHREADS 8

#include <tonemapping.h>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;
	uint i;

	float4 red;
	float4 green;
	float4 blue;

	const uint2 offsets[4] = {
        uint2(0, 0), uint2(1, 0),
        uint2(1, 0), uint2(1, 1),
    };

    [unroll]
	for(int t=0; t<4; t++) {
		float3 tap = 0;
		uint2 tap_pos = texel * 2 + offsets[t];

#if !MSAA_ENABLED
		tap = Source[tap_pos].xyz;
#else 
		[unroll]
		for(i=0; i< MS_SAMPLE_COUNT; i++) {
			tap += SourceMS.Load(tap_pos, i).xyz;
		}
		tap /= (float) MS_SAMPLE_COUNT;	
#endif
		red[t] = tap.x;
		green[t] = tap.y;
		blue[t] = tap.z;
	}

	float4 result = 0;
	[unroll]
	for(i=0; i< 4; i++) {
		float3 color = float3(red[i], green[i], blue[i]);
		result.xyz += max(exposed_color(color, frame_.luminance_exposure + frame_.bloom_exposure), 0); // nans sometimes
	}
	
	result /= 4.f;

	Destination[texel] = result;
}
