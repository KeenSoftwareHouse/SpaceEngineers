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

Texture2D<float2> PrevLum		: register( t1 );

static const uint NumThreads = NUMTHREADS_X * NUMTHREADS_Y;

#include <Frame.h>

cbuffer Constants : register( c1 )
{
	uint2 Texture_size;
	int Texture_texels;
	float Constant_luminance;
};


groupshared float2 ReduceBuffer[NumThreads];
