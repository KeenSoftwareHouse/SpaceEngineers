struct VertexStageOutput
{
	float4 position : SV_Position;
	MaterialVertexPayload custom;

	float4 key_color_alpha : TEXCOORD7;
	float custom_alpha : TEXCOORD9;
#if defined(BUILD_TANGENT_IN_PIXEL) || defined(WANTS_POSITION_WS)
	float3 position_ws : TEXCOORD8;
#endif

#ifdef USE_SIMPLE_INSTANCING_COLORING // all other key colors and and emissivities are deprecated for simple instancing
	float4 instance_key_color_dithering : TEXCOORD10;
	float4 instance_color_mult_emissivity : TEXCOORD11;
#endif
};

void __vertex_shader(__VertexInput input, out VertexStageOutput output, uint sv_vertex_id : SV_VertexID)
{
	VertexShaderInterface vertex = __prepare_interface(input, sv_vertex_id);

	vertex_program(vertex, output.custom);

	output.position = vertex.position_clip;
	output.key_color_alpha = float4(vertex.key_color, vertex.custom_alpha);
	output.custom_alpha = vertex.custom_alpha;

#ifdef USE_SIMPLE_INSTANCING_COLORING
	output.instance_key_color_dithering = input.instance_keyColorDithering;
	output.instance_color_mult_emissivity = input.instance_colorMultEmissivity;
#endif

#if defined(BUILD_TANGENT_IN_PIXEL) || defined(WANTS_POSITION_WS)
	output.position_ws = vertex.position_local.xyz;
#endif
}
