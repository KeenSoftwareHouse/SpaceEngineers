#include "Debug.hlsli"

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
    output = DebugTexture.SampleLevel(BilinearSampler, vertex.uv, 0);
}
