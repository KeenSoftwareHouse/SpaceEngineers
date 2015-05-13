#include <template.h>

struct LineVertex
{
	float4 position : POSITION;
	float4 color	: COLOR;
};

struct VertexInOut {
	float4 position	: SV_Position;
	float4 color	: COLOR;
};

void vs(LineVertex input, out VertexInOut output)
{
	output.position = mul(float4(input.position.xyz, 1), projection_.view_proj_matrix);
	output.color = input.color;
}

void ps(VertexInOut input, out float4 output : SV_Target0)
{
	output = input.color;
}