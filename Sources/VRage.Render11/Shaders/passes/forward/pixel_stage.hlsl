struct PixelStageInput
{
	float4 position : SV_Position;
	float3 worldPosition : POSITION;
	MaterialVertexPayload custom;
#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	float4 key_color_alpha : TEXCOORD7;
#endif
};

#include <brdf.h>
#include <Surface.h>
#include <LightingModel.h>

#define CUSTOM_CASCADE_SLOT
Texture2DArray<float> CSM : register( MERGE(t,60) );

#include <csm.h>

// csm
// point lights

SurfaceInterface surfaceFromMaterial(MaterialOutputInterface mat, float3 position) {
	SurfaceInterface surface;

	surface.base_color = mat.base_color;
	surface.metalness = mat.metalness;
	surface.gloss = mat.gloss;
	surface.albedo = surface_albedo(mat.base_color, mat.metalness);
	surface.f0 = surface_f0(mat.base_color, mat.metalness);
	surface.id = mat.id;
	surface.ao = mat.ao;
	surface.emissive = mat.emissive;
	surface.position = position;
	surface.positionView = mul(position, (float3x3) frame_.view_matrix);
	surface.N = mat.normal;
	surface.NView = mul(mat.normal, (float3x3) frame_.view_matrix);
	surface.V = normalize(get_camera_position() - position);
	surface.VView = mul(surface.V, (float3x3) frame_.view_matrix);
	surface.native_depth = 0;
	surface.depth = 0;
	surface.coverage = mat.coverage;
	surface.stencil = 0;

	surface.material_type = 0;

	return surface;
}

#ifdef CASCADES_NUM
float4 shade_forward(SurfaceInterface surface, float3 position) {
	float4 shaded = 0;

	float shadow = calculate_shadow_fast_aprox(position);
	shaded.xyz = main_directional_light(surface) * shadow;
	
	shaded.xyz = max(shaded.xyz, frame_.forwardPassAmbient * surface.albedo);
	shaded.w = 1;
	return shaded;
}
#endif

void __pixel_shader(PixelStageInput input, out float4 shaded : SV_Target0 ) {

	PixelInterface pixel;
	pixel.screen_position = input.position.xyz;
	pixel.custom = input.custom;
	init_ps_interface(pixel);
	pixel.position_ws = input.worldPosition;
#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	pixel.key_color = input.key_color_alpha.xyz;
	pixel.custom_alpha = input.key_color_alpha.w;
#endif

	MaterialOutputInterface material_output = make_mat_interface();
	pixel_program(pixel, material_output);
	if(material_output.DISCARD)
		discard;

	SurfaceInterface surface = surfaceFromMaterial(material_output, input.worldPosition);

	float4 csProjInMainScreen = mul(float4(input.worldPosition, 1), frame_.view_projection_matrix);
	csProjInMainScreen /= csProjInMainScreen.w;

	surface.native_depth = csProjInMainScreen.z;
	surface.depth = linearize_depth(surface.native_depth, frame_.projection_matrix);
	shaded = shade_forward(surface, input.worldPosition);
}