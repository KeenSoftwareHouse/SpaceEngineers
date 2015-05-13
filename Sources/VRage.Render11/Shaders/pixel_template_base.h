#include <template.h>

// TOKEN START
__MATERIAL_DECLARATIONS__
// TOKEN END

cbuffer Material : register( MERGE(b,MATERIAL_SLOT) )
{
	MaterialConstants material_;
};

Texture2D<float> Dither8x8 : register( MERGE(t,DITHER_8X8_SLOT) );

struct PixelInterface 
{
	float2 screen_position;
	float3 key_color;
	float custom_alpha;
	float3 color_mul;
	float emissive;
	float3 position_ws;
	uint material_index;
	uint material_flags;
	// USER DATA 
	MaterialVertexPayload custom;
};

void init_ps_interface(inout PixelInterface pix) {
	pix.key_color = 0;
	pix.custom_alpha = 0;
	pix.position_ws = 0;
	pix.color_mul = 1;
	pix.emissive = 0;
	pix.material_index = 0;
	pix.material_flags = 0;
#ifndef PASS_OBJECT_VALUES_THROUGH_STAGES
	pix.material_index = object_.material_index;
	pix.material_flags = object_.material_flags;
	pix.key_color = object_.key_color;
	pix.custom_alpha = object_.custom_alpha;
	pix.color_mul = object_.color_mul;
	pix.emissive = object_.emissive;
#endif
}

struct MaterialOutputInterface
{
	float3 base_color;
	float3 normal;
	float gloss;
	float metalness;
	float transparency;
	uint id;
	float ao;
	float emissive;
	uint coverage;
	bool DISCARD;
};

MaterialOutputInterface make_mat_interface()
{
	MaterialOutputInterface result;
	result.base_color = 0;
	result.normal = 0;
	result.gloss = 0;
	result.metalness = 0;
	result.transparency = 0;
	result.id = 0;
	result.ao = 1;
	result.emissive = 0;
	result.coverage = COVERAGE_MASK_ALL;
	result.DISCARD = 0;
	return result;
}

#define DISCARD_PIXEL \
	output.DISCARD = 1; \
	return;

// TOKEN START
__MATERIAL_PIXELPROGRAM__
// TOKEN END