#include "Debug.hlsli"

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	output = 0;
	if(gbuffer_edgedetect(vertex.position.xy)) {
		output = float4(1,0,0,1);
	}
	else {
		discard;
	}
}
