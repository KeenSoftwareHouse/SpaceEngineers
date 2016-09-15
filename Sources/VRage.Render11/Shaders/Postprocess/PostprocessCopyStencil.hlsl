#include "Postprocess.hlsli"
#include <Common.hlsli>

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    if ( Stencil[input.position.xy].g & 0x40 )
    {
        discard;
    }
    output = Source[input.position.xy];
#else
    [unroll]
    for(uint i=1; i<MS_SAMPLE_COUNT; i++)
    {
        if ( Stencil.Load(input.position.xy, i).g & 0x40 )
        {
            discard;
        }
    }
    output = Source.Load(input.position.xy, 0);
#endif
}
