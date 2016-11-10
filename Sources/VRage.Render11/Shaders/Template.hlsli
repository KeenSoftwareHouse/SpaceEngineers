#ifndef TEMPLATE_H
#define TEMPLATE_H

#include <Frame.hlsli>

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

float4 WorldToClip(float3 worldPosition)
{
    return mul(float4(worldPosition, 1), projection_.view_proj_matrix);
}

#define MATERIAL_FLAG_COLORING (MATERIAL_FLAG_HAS_KEYCOLOR)

#define MATERIAL_FLAG_NO_KEYCOLOR 2

struct ObjectConstants
{
#ifdef USE_MERGE_INSTANCING
    int instanceIndex;
    int startIndex;
    float __p1, __p2;
#else

#ifndef USE_VOXEL_DATA
    float   facing;
    float2 	windScaleAndFreq;
    float 	__paddingNonVoxelData;
    float3 CenterOffset;
    float __paddingNonVoxelData2;
#elif defined(USE_VOXEL_DATA)
    float   voxelLodSize;
    float3 	voxel_offset;
    float4 	massive_center_radius;
    float3 	voxel_scale;
    float 	__paddingVoxelData;
#endif
    float4 	matrix_row0;
    float4 	matrix_row1;
    float4 	matrix_row2;

    float3 	key_color;	// color mask, can be hsv offset or rgb (depends on type of coloring)
    float 	custom_alpha; // used for dithering, default = 0

    float3 	color_mul; 	// mostly for emissive, default = 1
    float 	emissive;		// default = 0

    uint    LOD;
	uint    depth_bias;
    uint    material_flags;
    float   __paddingCommon;

#ifdef USE_SKINNING
    matrix bone_matrix[60];
#endif

#endif
};

cbuffer Object : register( MERGE(b,OBJECT_SLOT) )
{
	ObjectConstants object_;
};

#ifndef USE_MERGE_INSTANCING

matrix get_object_matrix()
{
	return transpose(matrix(object_.matrix_row0, object_.matrix_row1, object_.matrix_row2, float4(0,0,0,1)));
}

#endif

#endif