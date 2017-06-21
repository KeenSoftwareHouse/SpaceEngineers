// @defineMandatory NUMTHREADS 8
// @define _FINAL

#include "Defines.hlsli"

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
		if(ThreadIndex < s) 
        {
			ReduceBuffer[ThreadIndex] += ReduceBuffer[ThreadIndex + s];
		}
		GroupMemoryBarrierWithGroupSync();
	}

    if ( ThreadIndex == 0 ) 
    {
#ifdef _FINAL
        float prevlum = PrevLum[uint2(0, 0)].x;
        prevlum = clamp(prevlum, 0, 100000);
        float sum = ReduceBuffer[0].x / ReduceBuffer[0].y;
        sum = max(sum, frame_.Post.LogLumThreshold);
        //sum = lerp(frame_.Post.LogLumThreshold, sum, ReduceBuffer[0].y / 50000);
#if MSAA_ENABLED
        //sum /= MS_SAMPLE_COUNT;
#endif
        float currentlum = exp(sum);
        prevlum = prevlum == 0 ? currentlum : prevlum;
        float adaptedlum = prevlum + (currentlum - prevlum) * (1 - exp(-frame_.Post.EyeAdaptationTau * frame_.frameTimeDelta));
        float exposure = CalculateExposure(adaptedlum, frame_.Post.LuminanceExposure);

        float2 output = float2(adaptedlum, exposure);
#else
        float2 output = ReduceBuffer[0];
#endif
        ReduceOutput[GroupID.xy] = output;
    }
}
