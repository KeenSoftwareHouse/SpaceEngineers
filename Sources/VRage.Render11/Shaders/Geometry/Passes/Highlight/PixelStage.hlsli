struct PixelStageInput
{
	float4 position : SV_Position;
	float3 worldPosition : POSITION;
	MaterialVertexPayload custom;
#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	float4 key_color_alpha : TEXCOORD7;
	float custom_alpha : TEXCOORD9;
#endif
};

struct OutlineConstants {
	float4 Color;
};

cbuffer OutlineConstants : register( b4 ) {
	OutlineConstants Outline;
};

void __pixel_shader(PixelStageInput input, out float4 shaded : SV_Target0 )
{
	PixelInterface pixel;
	pixel.screen_position = input.position.xyz;
	pixel.custom = input.custom;
	init_ps_interface(pixel);
	pixel.position_ws = input.worldPosition;

#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	pixel.key_color = input.key_color_alpha.xyz;
    pixel.custom_alpha = input.custom_alpha;
#endif

	MaterialOutputInterface material_output = make_mat_interface();
	pixel_program(pixel, material_output);

	//float4 volumePos = mul(float4(pixel.position_ws, 1), Outline.WorldToVolume);
	//volumePos /= volumePos.w;

	//if(any(abs(volumePos.xyz) > 0.5) )
	//	discard;

	shaded = Outline.Color;
}
