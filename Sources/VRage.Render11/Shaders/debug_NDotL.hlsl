#include <debug.h>

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(dot(input.N, -frame_.directionalLightVec).xxx, 1.0);
}
