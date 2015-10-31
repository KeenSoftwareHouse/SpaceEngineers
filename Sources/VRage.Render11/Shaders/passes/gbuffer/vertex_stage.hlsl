struct VertexStageOutput
{
	float4 position : SV_Position;
	MaterialVertexPayload custom;
	float4 key_color_alpha : TEXCOORD7;
#ifdef BUILD_TANGENT_IN_PIXEL
	float3 position_ws : TEXCOORD8;
#endif
};

void __vertex_shader(__VertexInput input, out VertexStageOutput output, uint sv_vertex_id : SV_VertexID)
{
	VertexShaderInterface vertex = __prepare_interface(input, sv_vertex_id);

	vertex_program(vertex, output.custom);

	output.position = vertex.position_clip;
	output.key_color_alpha = float4(vertex.key_color, vertex.custom_alpha);

#ifdef BUILD_TANGENT_IN_PIXEL
	output.position_ws = vertex.position_local.xyz;
#endif
}
