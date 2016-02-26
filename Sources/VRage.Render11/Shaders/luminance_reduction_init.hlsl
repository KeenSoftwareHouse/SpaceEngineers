// @define NUMTHREADS 8

#include <luminance_reduction.h>
#include <Math/Color.h>

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
	float ok = v > frame_.logLumThreshold;
	luminance += v * ok;
	validCtr += ok;
#else
	[unroll]
	for(uint i=0; i< MS_SAMPLE_COUNT; i++) {
		float3 sample = SourceMS.Load(texel, i).xyz;
		float v = log(max( calc_luminance(sample), 0.0001f));
		float ok = v > frame_.logLumThreshold;
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

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void luminance_reduce(
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


