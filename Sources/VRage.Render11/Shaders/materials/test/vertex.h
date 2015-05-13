void vertex_program(VertexInterface vertex, inout VertexStageOutputInterface output, out MaterialVertexPayload custom_output)
{
	set_world_position(output, vertex.position_world.xyz);

	custom_output.color = abs(vertex.position_world.xyz) / 50;
	custom_output.normal = vertex.normal_world;
}