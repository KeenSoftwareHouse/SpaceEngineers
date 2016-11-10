#include "Debug.hlsli"

void __vertex_shader(ScreenVertex vertex, out float4 position : SV_Position, out float2 texcoord : TEXCOORD0) 
{
	float2 xy = vertex.position / frame_.Screen.resolution;
	xy = xy * 2 - 1;
	xy.y = -xy.y;

	position = float4(xy, 0, 1);
	texcoord = vertex.texcoord;
}

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(input.base_color.xyz, 1);
}
