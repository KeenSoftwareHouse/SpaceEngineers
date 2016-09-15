#include "Debug.hlsli"

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    float depth = DepthBuffer[vertex.position.xy];
#else
    float depth = DepthBuffer.Load(vertex.position.xy, 0);
#endif

    output = depth * 5;
}
