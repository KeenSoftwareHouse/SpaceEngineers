#include "Shadows.hlsli"
#include "..\Postprocess\PostprocessBase.hlsli"


static const float4 ColorTable[] =
{
	float4(1, 0, 0, 1),
	float4(1, 1, 0, 1),
	float4(0, 1, 0, 1),
	float4(0, 1, 1, 1),
	float4(0, 0, 1, 1),
	float4(1, 0, 1, 1),
	float4(1, 0.5f, 1, 1),
	float4(0.5f, 1, 0, 1)
};


void __pixel_shader(PostprocessVertex vertex, out float4 outColor : SV_TARGET0)
{
	int3 pos = int3(vertex.position.xy, 0);
	uint stencil = Stencil.Load(pos, 0).r;
	uint cascadeId = GetCascadeIdFromStencil(stencil);
	outColor = ColorTable[cascadeId];
}