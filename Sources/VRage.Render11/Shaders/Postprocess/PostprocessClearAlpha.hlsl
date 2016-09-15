#include "Postprocess.hlsli"
#include <Common.hlsli>

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
	output = float4(0,0,0,1);
}
