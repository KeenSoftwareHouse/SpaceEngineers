#include "Debug.hlsli"

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

#ifndef MS_SAMPLE_COUNT
	float depth = DepthBuffer[vertex.position.xy];
#else
	float depth = DepthBuffer.Load(vertex.position.xy, 0);
#endif

	output = float4(ambient_diffuse(input.albedo, input.N), 1);
	//output = float4(ambient_diffuse(input.albedo, input.N, input.depth), 1);

	// Pure diffuse
	//output = float4(ambient_diffuse(input.N), 1);
}
