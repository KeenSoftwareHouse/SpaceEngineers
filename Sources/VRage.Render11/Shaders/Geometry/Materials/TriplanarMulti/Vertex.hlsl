#include "Declarations.hlsli"
#include <Common.hlsli>
#include <Math/Math.hlsli>
#include <Geometry/VertexTemplateBase.hlsli>

#define WANTS_POSITION_WS 1

void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
#if defined(DEPTH_ONLY) && defined(USE_VOXEL_DATA)
	// Because the ground has no thickness, we need to simulate some when the sun is underneath the surface
	if (object_.massive_center_radius.w > 0)
	{
		float normalLightDot = dot(vertex.normal_object, frame_.Light.directionalLightVec);
		if (normalLightDot > 0)
		{
            vertex.position_clip -= normalize(WorldToClip(frame_.Light.directionalLightVec)) * 0.15f;
		}
	}
#endif

	custom_output.normal = vertex.normal_object;
	custom_output.texcoords = vertex.position_scaled_untranslated.xyz;	
    custom_output.colorBrightnessFactor = vertex.colorBrightnessFactor;
	custom_output.world_matrix = vertex._local_matrix;

	float	dist		= length(vertex.position_local.xyz - get_camera_position());
	custom_output.distance = dist;

	uint4	matInfo		= vertex.triplanar_mat_info;
	float3	tmpCmpMask	= matInfo.yzz != matInfo.xxy ? 1 : 0;
    // this is mask to remove duplicated materials from triplet, so it is not blended-in multiple times
	float3	weightsMask = float3(1, tmpCmpMask.x, tmpCmpMask.y * tmpCmpMask.z); 
	custom_output.mat_indices = vertex.triplanar_mat_info.xyz;
	custom_output.mat_weights = matInfo.xyz == matInfo.w ? weightsMask : 0;
}

#include <Geometry/Passes/VertexStage.hlsli>
