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

#include "PostprocessBase.hlsli"
#include <Frame.hlsli>

void __pixel_shader(PostprocessVertex vertex, out float depth_write : SV_Depth)
{
	uint2 texel = vertex.position.xy;

	float output = 1;
#if !MSAA_ENABLED
	output = Source[texel].x;
#else
	[unroll]
	for(uint i=0; i< MS_SAMPLE_COUNT; i++) {
		float depth = SourceMS.Load(texel, i).x;
		output = min(output, depth);
	}
#endif

	depth_write = output;
}
