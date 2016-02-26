#include <Math/math.h>

void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
#if defined(DEPTH_ONLY) && defined(USE_VOXEL_DATA)
	// Because the ground has no thickness, we need to simulate some when the sun is underneath the surface
	if (object_.massive_center_radius.w > 0)
	{
		float normalLightDot = dot(vertex.normal_object, frame_.directionalLightVec);
		if (normalLightDot > 0)
		{
			vertex.position_clip -= normalize(world_to_clip(frame_.directionalLightVec)) * 0.15f;
		}
	}
#endif

	float dist = length(vertex.position_local.xyz - get_camera_position());
	custom_output.normal = vertex.normal_object;
	custom_output.texcoords = vertex.position_scaled_untranslated.xyz;
	custom_output.mat_weights = vertex.material_weights;
	custom_output.ambient_occlusion = vertex.ambient_occlusion;
	custom_output.distance = dist;
	custom_output.world_matrix = vertex._local_matrix;
	custom_output.dark_side = vertex.lDir.x;
}
