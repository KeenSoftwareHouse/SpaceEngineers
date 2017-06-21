struct PixelStageInput
{
	float4 position : SV_Position;
	MaterialVertexPayload custom;
};

struct Output 
{
	float4 render_target	: SV_Target0;
};

void __pixel_shader(PixelStageInput input, out Output output)
{
	PixelInterface pixel;
	pixel.screen_position = input.position.xyz;
	pixel.custom = input.custom;

	MaterialOutputInterface material_output;
	pixel_program(pixel, material_output);

    ApplyMultipliers(material_output);

	output.render_target = float4(material_output.base_color, 1);
}
