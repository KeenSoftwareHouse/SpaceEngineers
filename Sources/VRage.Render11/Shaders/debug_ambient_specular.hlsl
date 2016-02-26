#include <debug.h>

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.gloss), 1);

	if(input.native_depth == 1)
		discard;

	float3 N = input.N;
	float3 V = input.V;

	float3 f0 = SurfaceF0(input.base_color, input.metalness);
	output = float4(input.ao * ambient_specular(f0, input.gloss, N, V), 1);
	
	//  Pure specular
	//output = float4(ambient_specular(1.0f, 1.0f, N, V), 1.0f);
}
