void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
#if !defined(DEPTH_ONLY) || defined(ALPHA_MASKED)
	custom_output.texcoord0 = vertex.texcoord0;
#endif
#ifndef DEPTH_ONLY
	custom_output.normal = vertex.normal_world;
	custom_output.tangent = vertex.tangent_world;
#endif
}