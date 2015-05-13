struct VertexStageOutput
{
	float4 position : SV_Position;
	MaterialVertexPayload custom;
};

void __vertex_shader(__VertexInput input, out VertexStageOutput output)
{
	VertexInterface vertex = __prepare_interface(input);

	VertexStageOutputInterface output_interface;
	vertex_program(vertex, output_interface, output.custom);

	output.position = output_interface.position_clip;
}
