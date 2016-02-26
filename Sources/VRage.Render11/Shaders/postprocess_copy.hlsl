#include <common.h>
#include <postprocess.h>

void __vertex_shader(uint vertex_id : SV_VertexID, out PostprocessVertex vertex)
{
	vertex.position = float4(-1 + (vertex_id == 2) * 4, -1 + (vertex_id == 1) * 4, 0, 1);
	vertex.uv = float2(0 + (vertex_id == 2) * 2, 1 - 2 * (vertex_id == 1));
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    output = Source[input.position.xy];
#else
    output = Source.Load(input.position.xy, 0);
#endif
}
