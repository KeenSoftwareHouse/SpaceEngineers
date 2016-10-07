#include "..\Postprocess\PostprocessBase.hlsli"

Texture2D<float4> Input : register(t0);

void __pixel_shader(PostprocessVertex vertex, out float4 outColor : SV_TARGET0)
{
	int3 pos = int3(vertex.position.xy, 0);
	float v = Input.Load(pos, 0).r;
	outColor = float4(v, v, v, v);;
}