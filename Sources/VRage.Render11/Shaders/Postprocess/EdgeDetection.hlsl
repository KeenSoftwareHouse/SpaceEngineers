#include "PostprocessBase.hlsli"
#include <GBuffer/GBuffer.hlsli>
#include <Math/Math.hlsli>

void __pixel_shader(PostprocessVertex vertex)
{
	if(!gbuffer_edgedetect(vertex.position.xy))
		discard;
}
