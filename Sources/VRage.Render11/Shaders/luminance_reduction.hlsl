#ifndef NUMTHREADS
#define NUMTHREADS 1
#endif

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

#ifdef MS_SAMPLE_COUNT
#define MSAA_ENABLED 1
#else
#define MSAA_ENABLED 0
#endif

#if !MSAA_ENABLED
Texture2D<float4> Source : register( t0 );
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> SourceMS : register( t0 );
#endif

RWTexture2D<float4> Destination	: register( u0 );

Texture2D<float2> ReduceInput : register( t0 );
RWTexture2D<float2> ReduceOutput	: register( u0 );

RWTexture2D<float2> LocalAvgLum	: register( u1 );
Texture2D<float2> PrevLum		: register( t1 );

static const uint NumThreads = NUMTHREADS_X * NUMTHREADS_Y;

#include <Frame.h>

cbuffer Constants : register( c1 )
{
	uint2 Texture_size;
	int Texture_texels;
};

#include <math.h>

groupshared float2 ReduceBuffer[NumThreads];

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void luminance_init(
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
        float adaptedlum = prevlum + (currentlum - prevlum) * saturate(1 - exp(-frame_.timedelta * frame_.tau));

		ReduceOutput[GroupID.xy] = adaptedlum;
#else
		ReduceOutput[GroupID.xy] = ReduceBuffer[0];
#endif
	}        
}


