#include <common.h>
#include <frame.h>

#if defined(USE_MERGE_INSTANCING) || defined(USE_CUBE_INSTANCING) || defined(USE_DEFORMED_CUBE_INSTANCING) || defined(USE_GENERIC_INSTANCING)
	#define PASS_OBJECT_VALUES_THROUGH_STAGES
#endif

#ifdef USE_DEFORMED_CUBE_INSTANCING
	#define BUILD_TANGENT_IN_PIXEL
#endif

struct ProjectionConstants
{
	matrix view_proj_matrix;
};

cbuffer Projection : register( MERGE(b,PROJECTION_SLOT) )
{
	ProjectionConstants projection_;
};

float4x4 get_view_proj_matrix()
{
	return projection_.view_proj_matrix;
}

float4 world_to_clip(float3 p)
{
    return mul(float4(p, 1), projection_.view_proj_matrix);
}

#define MATERIAL_FLAG_RGB_COLORING 1

struct ObjectConstants
{
	float4 	matrix_row0;
	float4 	matrix_row1;
	float4 	matrix_row2;
	float3 	key_color;	// color mask, can be hsv offset or rgb (depends on type of coloring)
	float 	custom_alpha; // used for dithering, default = 0

	float3 	color_mul; 	// mostly for emissive, default = 1
	float 	emissive;		// default = 0
	uint 	material_index; // 0-255 material id (mostly for brdf etc), we pack it in gbuffer!
	uint 	material_flags;

	float2 	__padding0;
	float3 	voxel_offset;
	float 	__padding1;
	float3 	voxel_scale;
	float 	__padding2;

#ifdef USE_SKINNING
	matrix bone_matrix[60];
#endif
};

cbuffer Object : register( MERGE(b,OBJECT_SLOT) )
{
	ObjectConstants object_;
};

matrix get_object_matrix()
{
	return transpose(matrix(object_.matrix_row0, object_.matrix_row1, object_.matrix_row2, float4(0,0,0,1)));
}
