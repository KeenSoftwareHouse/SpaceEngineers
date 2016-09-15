#include "Shadows.hlsli"
#include "..\Postprocess\PostprocessBase.hlsli"


float __pixel_shader(PostprocessVertex vertex) : SV_Target0
{
	uint2 screencoord = vertex.position.xy;
	float2 screenUV = screen_to_uv(screencoord);

	uint cascadeIndex = GetCascadeIdFromStencil(Stencil[screencoord].r);
	if (cascadeIndex >= MAX_CASCADES_COUNT)
		return 1;

	matrix m = cascades[cascadeIndex].worldToShadowSpace;
	float3 worldPos = ReconstructWorldPosition(Depth[screencoord].r, screenUV);
	worldPos += GetShadowNormalOffset(screencoord, cascadeIndex);

	return GetSimpleShadow(m, worldPos, cascadeIndex);
}