#include <Frame.hlsli>
#include <Math/Color.hlsli>

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

Texture2D<float4> Source : register( t0 );
Texture2D<float4> SourceGBuffer2 : register(t1);
Texture2D<float4> SourceDepth : register(t2);

RWTexture2D<float4> Destination : register( u0 );

SamplerState BilinearSampler : register( s0 );
