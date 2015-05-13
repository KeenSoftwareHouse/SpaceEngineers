#include <frame.h>
#include <math.h>

float4 unpack_position_and_scale(float4 position)
{
	return float4(position.xyz * position.w, 1);
}

float3 unpack_normal(float4 p)
{
	float zsign = p.y > 0.5f ? 1 : -1;
	if(zsign > 0) p.y -= 0.5f;
	float2 xy = 256 * (p.xz + 256 * p.yw);		
	xy /= 32767;
	xy = 2 * xy - 1;
	return float3(xy.xy, zsign * sqrt(saturate(1-dot(xy, xy))));
}

#include <D3DX_DXGIFormatConvert.inl>

struct Vertex {
	float4 position_packed : POSITION;
	float2 a : TEXCOORD0;
	float4 normal_packed : NORMAL;
	float4 b : TANGENT;
};

struct InstanceData {
	float4 row0;
	float4 row1;
	float4 row2;
};

StructuredBuffer<InstanceData> InstanceDataSRV : register(t0);

struct VertexToPixel {
	float4 position : SV_Position;
	float3 normal : NORMAL;
	float3 color : COLOR;
};

static const float3 DEBUG_COLORS_LIST [] = {
	{ 1, 0, 0 },
	{ 0, 1, 0 },
	{ 0, 0, 1 },

	{ 1, 1, 0 },
	{ 0, 1, 1 },
	{ 1, 0, 1 },

	{ 0.5, 0, 1 },
	{ 0.5, 1, 0 },

	{ 1, 0, 0.5 },
	{ 0, 1, 0.5 },

	{ 1, 0.5, 0 },
	{ 0, 0.5, 1 },

	{ 0.5, 1, 1 },
	{ 1, 0.5, 1 },
	{ 1, 1, 0.5 },
	{ 0.5, 0.5, 1 },	
};

static const uint DEBUG_COLORS_NUM = 16;

void vs(
	Vertex vertex,
	uint sv_instance : SV_InstanceID, 
	out VertexToPixel output)
{
	InstanceData instance_data = InstanceDataSRV[sv_instance];
	matrix instance_matrix = transpose(matrix(instance_data.row0, instance_data.row1, instance_data.row2, float4(0,0,0,1)));

	
	float4 position = unpack_position_and_scale(vertex.position_packed);
	position = mul(position, mul(instance_matrix, frame_.view_projection_matrix));

	float3 normal = unpack_normal(vertex.normal_packed);

	output.position = position;
	output.normal = normal;	
	output.color = DEBUG_COLORS_LIST[sv_instance % DEBUG_COLORS_NUM];
}

#include <gbuffer_write.h>

void ps(VertexToPixel vertex, out GbufferOutput output)
{
	gbuffer_write(output, vertex.color, 0, 0, vertex.normal, 0);
}