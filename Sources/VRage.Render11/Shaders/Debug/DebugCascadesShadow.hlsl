#include "Debug.hlsli"

static const float3 cascadeColor[] = {
	float3(1,0,0),
	float3(0,1,0),
	float3(0,0,1),
	float3(1,1,0),

	float3(0,1,1),
	float3(1,0,1),
	float3(1,0,0.5),
	float3(0.5,1,0),
};

Texture2D<float> Shadows : register( MERGE(t,SHADOW_SLOT) );

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

    float shadow = calculate_shadow(input.position, input.stencil);
	shadow = 0.5f * shadow + 0.5f;
	output = float4(cascadeColor[cascade_id_stencil(input.stencil)] * shadow, 1);
}
