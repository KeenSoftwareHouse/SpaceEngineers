#include <debug.h>

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	float3 albedo = SurfaceAlbedo(input.base_color, input.metalness);
	output = float4(input.ao * ambient_diffuse(input.N) * albedo, 1);

	// Pure diffuse
	//output = float4(ambient_diffuse(input.N), 1);
}
