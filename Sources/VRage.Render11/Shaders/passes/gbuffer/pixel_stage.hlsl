struct PixelStageInput
{
	float4 position : SV_Position;
	MaterialVertexPayload custom;
#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	float4 key_color_alpha : TEXCOORD7;
#endif
#ifdef BUILD_TANGENT_IN_PIXEL
	float3 position_ws : TEXCOORD8;
#endif
};

#include <gbuffer_write.h>

void __pixel_shader(PixelStageInput input, out GbufferOutput output,
	 uint coverage : SV_Coverage) {

	PixelInterface pixel;
	pixel.screen_position = input.position.xy;
	pixel.custom = input.custom;
	init_ps_interface(pixel);
#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	pixel.key_color = input.key_color_alpha.xyz;
	pixel.custom_alpha = input.key_color_alpha.w;
#endif
#ifdef BUILD_TANGENT_IN_PIXEL
	pixel.position_ws = input.position_ws;
#endif

	MaterialOutputInterface material_output = make_mat_interface();
	material_output.coverage = coverage;
	
	pixel_program(pixel, material_output);
	if(material_output.DISCARD)
		discard;

	gbuffer_write(output, material_output.base_color, material_output.metalness, material_output.gloss, material_output.normal, material_output.id, material_output.ao, material_output.emissive, material_output.coverage);
}