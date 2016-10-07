#include "PostprocessBase.hlsli"

#ifndef MS_SAMPLE_COUNT
Texture2D Source : register(t0);
Texture2D<uint2> Stencil : register(t1);
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> Source : register(t0);
Texture2DMS<uint2, MS_SAMPLE_COUNT> Stencil : register(t1);
#endif
