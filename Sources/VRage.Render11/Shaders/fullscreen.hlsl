#include "Common.h"

void fullscreen_triangle(uint vertex_id : SV_VertexID, out PostprocessVertex vertex, inout uint instance_id : SV_InstanceID)
{
	vertex.position = float4(-1 + (vertex_id == 2) * 4, -1 + (vertex_id == 1) * 4, 0, 1);
	vertex.uv = float2(0 + (vertex_id == 2) * 2, 1 - 2 * (vertex_id == 1));
}
