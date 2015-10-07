#include <math.h>

void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
	//material_.extension_detail_scale
	/*float4 ext = Ext_BottomSides_Up[0].SampleLevel(TextureSampler, 0.00005f * float3((vertex.position_scaled_untranslated * object_.voxel_scale + object_.voxel_offset).zx, 0), 0);
	vertex.position_local.xyz += 0.3f * ext.x * vertex.normal_world;
	vertex.position_clip = mul(vertex.position_local, vertex._view_proj_matrix);*/

#ifdef DEPTH_ONLY
	// Because the ground has no thickness, we need to simulate some when the sun is underneath the surface
	if (object_.massive_center_radius.w > 0 && dot(vertex.normal_object, frame_.directionalLightVec) > 0)
	{
		vertex.position_clip -= normalize(world_to_clip(vertex.normal_object)) * 0.5;
	}
#endif

	float dist = length(vertex.position_local.xyz - get_camera_position());
	custom_output.normal = vertex.normal_object;
	custom_output.texcoords = vertex.position_scaled_untranslated.xyz;
	custom_output.ambient_occlusion = vertex.ambient_occlusion;
	custom_output.distance = dist;
	custom_output.world_matrix = vertex._local_matrix;
}
