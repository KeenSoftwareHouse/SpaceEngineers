struct VertexStageIn
{
	float2 position : POSITION;
};

struct PixelStageIn
{
	float4 position : SV_Position;
};

#include <Template.hlsli>
#include <Math/Math.hlsli>

void __vertex_shader(VertexStageIn input, out PixelStageIn output)
{
	output.position = float4(input.position.xy,1,1);
}

void __pixel_shader(PixelStageIn input, out float4 rt : SV_Target0)
{
	rt = float4(1,0.5f,0.75f,1.0f);
}
