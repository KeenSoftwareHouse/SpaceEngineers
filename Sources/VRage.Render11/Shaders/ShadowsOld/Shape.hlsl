struct VertexStageIn
{
	float3 position : POSITION;
};

struct PixelStageIn
{
	float4 position : SV_Position;
};

#include <Template.hlsli>
#include <Math/Math.hlsli>

void __vertex_shader(VertexStageIn input, out PixelStageIn output)
{
	output.position = mul(float4(input.position.xyz, 1), frame_.Environment.view_projection_matrix);
}

void __pixel_shader(PixelStageIn input)
{
	
}
