void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
#ifndef DEPTH_ONLY
	custom_output.texcoord0 = vertex.texcoord0;	
	custom_output.local_forward = -vertex._local_matrix._31_32_33;
    custom_output.local_up = vertex._local_matrix._21_22_23;
	custom_output.normal = vertex.normal_world;
	custom_output.tangent = vertex.tangent_world;	
#endif
}