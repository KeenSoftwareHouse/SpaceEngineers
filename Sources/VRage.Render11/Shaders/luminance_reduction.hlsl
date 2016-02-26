// @defineMandatory NUMTHREADS 8
// @define _FINAL

#include <luminance_reduction.h>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;

	float2 luminance = ReduceInput[texel];

	ReduceBuffer[ThreadIndex] = luminance;
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
#ifdef _FINAL
		float prevlum = PrevLum[uint2(0, 0)].x;
		prevlum = clamp(prevlum, 0, 100000);
		float sum = ReduceBuffer[0].x / ReduceBuffer[0].y;
		sum = max(sum, frame_.logLumThreshold);
		//sum = lerp(frame_.logLumThreshold, sum, ReduceBuffer[0].y / 50000);
#if MSAA_ENABLED
		//sum /= MS_SAMPLE_COUNT;
#endif
		float currentlum = exp(sum);
		prevlum = prevlum == 0 ? currentlum : prevlum;

		//TODO(AF) hotfix for lack of eye adaptation; because game calls MyCommon::UpdateFrameConstants multiple times per frame, frame_.deltatime is always zero here
        float adaptedlum = prevlum + (currentlum - prevlum) * saturate(1 - exp(-0.016 * frame_.tau));

		ReduceOutput[GroupID.xy] = adaptedlum;
#else
		ReduceOutput[GroupID.xy] = ReduceBuffer[0];
#endif
		if (Constant_luminance > 0.0f)
		{
			ReduceOutput[GroupID.xy] = Constant_luminance;
		}
	}        
}


