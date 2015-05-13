#include <math.h>

void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
	float dist = length(vertex.position_local.xyz - get_camera_position());
	custom_output.normal = vertex.normal_object;
	custom_output.texcoords = vertex.position_scaled_untranslated.xyz;
	custom_output.mat_weights = vertex.material_weights;
	custom_output.distance = dist;
}