#include "Billboards.hlsli"

#include <Frame.hlsli>
#include <VertexTransformations.hlsli>

struct VsOut
{
    float4 position : SV_Position;
    float2 texcoord : Texcoord0;
    uint   index : Texcoord1;
    float3 wposition : TEXCOORD2;
};

VsOut __vertex_shader(VsIn vertex, uint vertex_id : SV_VertexID)
{
    float4 projPos;
    uint billboard_index;
    CalculateVertexPosition(vertex, vertex_id, projPos, billboard_index);

    VsOut result;
    result.position = projPos;
    result.texcoord = vertex.texcoord;
    result.index = billboard_index;
    result.wposition = vertex.position.xyz;

	return result;
}

void __pixel_shader(VsOut vertex, out float2 normal : SV_TARGET0)
{
    // Set normals on gbuffer1 copy
    float3 nview = world_to_view(BillboardBuffer[vertex.index].normal);
    normal = pack_normals2(normalize(nview));
}
