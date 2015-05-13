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

struct VertexPosition {
	uint packed_xy; // 2x half
	uint packed_zw; // 2x half
};

struct VertexNormal {
	uint packed_unorm4;
	uint shit0;
	uint shit1;
};

struct InstanceIndirection {
	uint mesh_id;
	uint id;
};

struct InstanceData {
	float4 row0;
	float4 row1;
	float4 row2;
};

static const uint VERTEX_PAGE_SIZE = 36;

StructuredBuffer<InstanceIndirection> InstanceIndirectionSRV : register(t0);
StructuredBuffer<InstanceData> InstanceDataSRV : register(t1); // or cb?

StructuredBuffer<VertexPosition> VertexPositionSRV : register(t2);
StructuredBuffer<VertexNormal> VertexNormalSRV : register(t3);

float4 srv_unpack_position(uint packed_xy, uint packed_zw)
{
	float4 f4 = float4(
		f16tof32(packed_xy),
		f16tof32(packed_xy >> 16),
		f16tof32(packed_zw),
		f16tof32(packed_zw >> 16));
	return unpack_position_and_scale(f4);
}

struct VertexToPixel {
	float4 position : SV_Position;
	float3 normal : NORMAL;
	float3 color : COLOR;
	float3 someShit : TEXCOORD0;
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

void vs(uint sv_vertex_id : SV_VertexID, out VertexToPixel output)
{
	uint instance_offset = sv_vertex_id / VERTEX_PAGE_SIZE;
	uint vertex_id = sv_vertex_id % VERTEX_PAGE_SIZE;

	InstanceIndirection instance_ind = InstanceIndirectionSRV[instance_offset];
	InstanceData instance_data = InstanceDataSRV[instance_ind.id];
	matrix instance_matrix = transpose(matrix(instance_data.row0, instance_data.row1, instance_data.row2, float4(0,0,0,1)));

	VertexPosition packed_pos = VertexPositionSRV[instance_ind.mesh_id * VERTEX_PAGE_SIZE + vertex_id];
	float4 position = srv_unpack_position(packed_pos.packed_xy, packed_pos.packed_zw);

	position = mul(position, mul(instance_matrix, frame_.view_projection_matrix));

	VertexNormal packed_nor = VertexNormalSRV[instance_ind.mesh_id * VERTEX_PAGE_SIZE + vertex_id];
	float3 normal = unpack_normal(D3DX_R8G8B8A8_UNORM_to_FLOAT4(packed_nor.packed_unorm4));

	output.position = position;
	output.normal = normal;	
	output.color = DEBUG_COLORS_LIST[instance_offset % DEBUG_COLORS_NUM];
	output.someShit = float3(packed_nor.shit0, packed_nor.shit1, 0);
}

#include <gbuffer_write.h>

void ps(VertexToPixel vertex, out GbufferOutput output)
{
	gbuffer_write(output, vertex.color, 0, 0, vertex.normal, 0);
}