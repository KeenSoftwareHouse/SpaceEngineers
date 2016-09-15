#ifndef VERTEX_MERGE_INSTANCING_H__
#define VERTEX_MERGE_INSTANCING_H__

static const uint INDEX_PAGE_SIZE = 36;

struct VertexPosition {
	uint packed_xy; // 2x half
	uint packed_zw; // 2x half
};

struct VertexPacked {
	uint packed_norm;
	uint packed_tan;
	uint packed_uv;
};

struct InstanceIndirection {
	uint mesh_id;
	uint id; // 
};

struct InstanceData {
	float4 row0;
	float4 row1;
	float4 row2;
    uint DepthBias;
    float3 __padding;
};

StructuredBuffer<uint> IndexDataSRV : register(MERGE(t,BIG_TABLE_INDICES));
StructuredBuffer<VertexPosition> VertexPositionSRV : register(MERGE(t,BIG_TABLE_VERTEX_POSITION));
StructuredBuffer<VertexPacked> VertexPackedSRV : register(MERGE(t,BIG_TABLE_VERTEX));

StructuredBuffer<InstanceIndirection> InstanceIndirectionSRV : register(MERGE(t,INSTANCE_INDIRECTION));
StructuredBuffer<InstanceData> InstanceDataSRV : register(MERGE(t,INSTANCE_DATA));

float4 srv_unpack_position(uint packed_xy, uint packed_zw)
{
	float4 f4 = float4(
		f16tof32(packed_xy),
		f16tof32(packed_xy >> 16),
		f16tof32(packed_zw),
		f16tof32(packed_zw >> 16));
	return unpack_position_and_scale(f4);
}

#endif