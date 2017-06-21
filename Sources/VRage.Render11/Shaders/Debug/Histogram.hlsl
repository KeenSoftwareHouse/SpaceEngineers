// @define NUMTHREADS 8

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

RWTexture2D<uint> Histogram	: register( u0 );

static const uint NumThreads = NUMTHREADS_X * NUMTHREADS_Y;

#include <Frame.hlsli>
#include <Math/Color.hlsli>

cbuffer Constants : register( c1 )
{
	uint2 Texture_size;
	int Texture_texels;
};

static const float HISTOGRAM_MIN = -8.0f;
static const float HISTOGRAM_MAX = 8.0f;
static const float HISTOGRAM_SPAN = HISTOGRAM_MAX - HISTOGRAM_MIN;
static const uint HISTOGRAM_BINS = 512;

void store_value(float x) {
	//if(x > EXP_LUM_THRESHOLD) {
		uint bin = clamp((x - HISTOGRAM_MIN) / HISTOGRAM_SPAN * HISTOGRAM_BINS, 0, HISTOGRAM_BINS - 1);
		uint prev;
		InterlockedAdd(Histogram[int2(bin,0)], 1, prev);

        if (x > frame_.Post.LogLumThreshold)
        {
			uint tmp;
			InterlockedMax(Histogram[int2(512,0)], prev + 1, tmp);
		}
	//}
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;
	
	if(all(texel < Texture_size)) {

	#if !MSAA_ENABLED
		float3 sample = Source[texel].xyz;
		store_value(log(max( calc_luminance(sample), 0.0001f)));
	#else
		[unroll]
		for(uint i=0; i< MS_SAMPLE_COUNT; i++) {
			float3 sample = SourceMS.Load(texel, i).xyz;
			store_value(log(max( calc_luminance(sample), 0.0001f)));
		}
	#endif
		
	}
}