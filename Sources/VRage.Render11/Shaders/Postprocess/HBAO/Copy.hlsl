#include <Common.hlsli>
#include <Postprocess/Postprocess.hlsli>

void __vertex_shader(float4 pos : POSITION, float2 tex : TEXCOORD0, out PostprocessVertex vertex)
{
	vertex.position = pos;
	vertex.uv = tex;
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    output = abs(Source[input.position.xy]);
#else
    output = Source.Load(input.position.xy, 0);
#endif
}
