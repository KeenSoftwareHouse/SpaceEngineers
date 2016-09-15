// @define NUMTHREADS 8

#include "Defines.hlsli"
#include <Math/Color.hlsli>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;

	float luminance = 0;
	float validCtr = 0;
	
#if !MSAA_ENABLED
	float3 sample = Source[texel].xyz;
	float v = log(max( calc_luminance(sample), 0.0001f));
    float ok = v > frame_.Post.LogLumThreshold;
	luminance += v * ok;
	validCtr += ok;
#else
	[unroll]
	for(uint i=0; i< MS_SAMPLE_COUNT; i++) {
		float3 sample = SourceMS.Load(texel, i).xyz;
		float v = log(max( calc_luminance(sample), 0.0001f));
        float ok = v > frame_.Post.LogLumThreshold;
		luminance += v * ok;
		validCtr += ok;
	}
#endif

	ReduceBuffer[ThreadIndex].x = luminance;
	ReduceBuffer[ThreadIndex].y = validCtr;
    GroupMemoryBarrierWithGroupSync();

    [unroll]
	for(uint s = NumThreads / 2; s > 0; s >>= 1)
    {
		if(ThreadIndex < s) {
			ReduceBuffer[ThreadIndex] += ReduceBuffer[ThreadIndex + s];
		}
		GroupMemoryBarrierWithGroupSync();
	}

    if(ThreadIndex == 0) {
        ReduceOutput[GroupID.xy] = ReduceBuffer[0];
    }
}
