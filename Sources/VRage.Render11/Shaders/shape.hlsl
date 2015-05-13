struct VertexStageIn
{
	float3 position : POSITION;
};

struct PixelStageIn
{
	float4 position : SV_Position;
};

#include <template.h>
#include <math.h>

void vs(VertexStageIn input, out PixelStageIn output)
{
	output.position = mul(float4(input.position.xyz, 1), frame_.view_projection_matrix);
}

void ps_dummy(PixelStageIn input)
{
	
}