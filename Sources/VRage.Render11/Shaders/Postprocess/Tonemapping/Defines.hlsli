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

Texture2D<float4> Source : register( t0 );

Texture2D<float4> Bloom : register( t2 );

RWTexture2D<float4> Destination : register( u0 );

SamplerState BilinearSampler : register( s0 );
