struct VertexStageIn
{
	float4 position : POSITION;
	float4 color	: COLOR;
};

struct PixelStageIn
{
	float4 position : SV_Position;
	float4 color 	: COLOR;
};

#include <template.h>
#include <math.h>

void vs(VertexStageIn input, out PixelStageIn output)
{
	output.position = mul(float4(input.position.xyz, 1), projection_.view_proj_matrix);
	output.color = input.color;
}

void ps(PixelStageIn input, out float4 rt : SV_Target0)
{
	rt = input.color;
}