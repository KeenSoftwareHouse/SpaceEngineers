#include "Debug.hlsli"

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    int2 stencilSample = Stencil[vertex.position.xy];
#else
    int2 stencilSample = Stencil.Load(vertex.position.xy, 0);
#endif
    int stencilValue = stencilSample.g;
    output = 0.5f * float4(stencilValue & 0x01 + stencilValue & 0x02, stencilValue & 0x04 + stencilValue & 0x08 + stencilValue & 0x80, stencilValue & 0x10 + stencilValue & 0x20 + stencilValue & 0x40, stencilSample.g);
}
