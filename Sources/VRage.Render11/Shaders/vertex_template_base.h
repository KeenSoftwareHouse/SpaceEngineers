#include <template.h>

/////////////////////////////////////////////////////

struct VertexShaderInterface {
	// inout
	float4 position_local;
	float4 position_clip;

	float2 texcoord0;
	float3 material_weights;

	float3 normal_object;
	float3 normal_world;

	float4 tangent_object;
	float4 tangent_world;

	float3 key_color;
	float custom_alpha;


	// in
	float morphing;
	float4 position_scaled_untranslated; // for triplanadr mapping
	matrix _local_matrix;
	matrix _view_proj_matrix;
};

void set_position(inout VertexShaderInterface obj, float4 position)
{
	obj.position_local = position;
	obj.position_clip = mul(position, obj._view_proj_matrix);
}

VertexShaderInterface make_vs_interface()
{
	VertexShaderInterface data;

	data.position_local = 0;
	data.position_clip = 0;
	data.position_scaled_untranslated = 0;
	data.texcoord0 = 0;
	data.material_weights = 0;
	data.normal_object = 0;
	data.normal_world = 0;
	data.tangent_object = 0;
	data.tangent_world = 0;
	data.key_color = 0;
	data.custom_alpha = 0;
	data.morphing = 0;

	return data;
}


#include <vertex_transformations.h>
#include <vertex_merge_instancing.h>

matrix construct_matrix_43(float4 a, float4 b, float4 c) {
	return transpose(matrix(a, b, c, float4(0,0,0,1)));
}

float3 get_translation(matrix m) {
	return m._41_42_43;
}

// TOKEN START
__VERTEXINPUT_DECLARATIONS__
// TOKEN END

VertexShaderInterface __prepare_interface(__VertexInput input, uint sv_vertex_id = 0, uint sv_instance_id = 0) {
	//
	float4 __position_object = 0;	
	float4 __color = 0;	
	float3 __material_weights = 0;
	float2 __texcoord0 = 0;	
	float3 __normal = 0;
	float4 __tangent = 0;
	// morphing
	float4 __position_object_morph = 0;
	float3 __normal_morph = 0;
	float3 __material_weights_morph = 0;
	// skinning
	uint4  __blend_indices = 0;
	float4 __blend_weights = 0;
	// instancing
	matrix __instance_matrix;
	// cube instancing (deformation needs bones, processing need to be deferred to after loading all data from vertex!)
	float4 __packed_bone0;
	float4 __packed_bone1;
	float4 __packed_bone2;
	float4 __packed_bone3;
	float4 __packed_bone4;
	float4 __packed_bone5;
	float4 __packed_bone6;
	float4 __packed_bone7;
	float4 __cube_transformation;
	//
	float4 __colormask = 1;

	// TOKEN START
	__VERTEXINPUT_TRANSFER__
	// TOKEN END

	//
	VertexShaderInterface result = make_vs_interface();
	matrix local_matrix = get_object_matrix();
	matrix view_proj = projection_.view_proj_matrix;
	float3x3 normal_matrix = (float3x3)local_matrix;

#ifdef USE_MERGE_INSTANCING
	// we load data from srv here 
	uint instance_offset = sv_vertex_id / INDEX_PAGE_SIZE;
	uint index_id = sv_vertex_id % INDEX_PAGE_SIZE;
	InstanceIndirection instance_ind = InstanceIndirectionSRV[instance_offset];
	InstanceData instance_data = InstanceDataSRV[instance_ind.id];
	matrix instance_matrix = construct_matrix_43(instance_data.row0, instance_data.row1, instance_data.row2);

	uint index = IndexDataSRV[instance_ind.mesh_id * INDEX_PAGE_SIZE + index_id];

	VertexPosition packed_pos = VertexPositionSRV[index];
	__position_object = srv_unpack_position(packed_pos.packed_xy, packed_pos.packed_zw);

	local_matrix = instance_matrix;
	normal_matrix = (float3x3) local_matrix;
	// hack for now :(
	local_matrix._41_42_43 -= frame_.world_offset.xyz;

	#ifndef DEPTH_ONLY
		VertexPacked packed_vert = VertexPackedSRV[index];
		__normal = unpack_normal(D3DX_R8G8B8A8_UNORM_to_FLOAT4(packed_vert.packed_norm));
		__tangent = unpack_tangent_sign(D3DX_R8G8B8A8_UNORM_to_FLOAT4(packed_vert.packed_tan));
		__texcoord0 = float2(
			f16tof32(packed_vert.packed_uv),
			f16tof32(packed_vert.packed_uv >> 16));
	#endif
#endif

	// ALL DATA LOADED - from srv or vertex buffer

	float4 position_object = __position_object;
	matrix world_view_proj = mul(local_matrix, view_proj);

// MORPHING VOXEL

#ifdef USE_VOXEL_MORPHING
	
	float morphing = 0;
	{
		float3 local_position = mul(position_object, local_matrix).xyz;
		morphing = voxel_morphing(local_position, get_voxel_lod_range(object_.custom_alpha), get_camera_position());
		result.morphing = morphing;
	}
	position_object = lerp(__position_object, __position_object_morph, morphing);
	__normal = normalize(lerp(__normal, __normal_morph, morphing));
	__material_weights = lerp(__material_weights, __material_weights_morph, morphing);
#endif

	// CHANGING OBJECT POSITION WITH SKINNING

#ifdef USE_SKINNING
	matrix _skinning_matrix = 0;
	
	[unroll]
    for (int i = 0; i < 4; i++) 
    {
        _skinning_matrix += object_.bone_matrix[__blend_indices[i]] * __blend_weights[i];
    }
    position_object = mul(position_object, _skinning_matrix);
    position_object /= position_object.w;
    normal_matrix = mul((float3x3)_skinning_matrix, normal_matrix);
#endif

    // FILLING INSTANCE DATA

    float4 position_instance = position_object;

#ifdef USE_CUBE_INSTANCING
	__instance_matrix = construct_cube_instance_matrix(__cube_transformation);
#endif

#ifdef USE_DEFORMED_CUBE_INSTANCING
	__texcoord0 = __texcoord0 + float2(__packed_bone5.w, __packed_bone6.w);
	__instance_matrix = construct_deformed_cube_instance_matrix(__packed_bone0, __packed_bone1, __packed_bone2, __packed_bone3, __packed_bone4, __packed_bone5, __packed_bone6, __packed_bone7, __cube_transformation, __blend_indices, __blend_weights);
#endif

#if defined(USE_GENERIC_INSTANCING) || defined(USE_CUBE_INSTANCING) || defined(USE_DEFORMED_CUBE_INSTANCING)
	position_instance = mul(position_instance, __instance_matrix);
	normal_matrix = mul((float3x3)__instance_matrix, normal_matrix);
#endif

	// FINAL TRANSFORMATIONS
	// position instance is position in local space
	// local and world space are the same for shaders

	result.position_local = mul(position_instance, local_matrix);
	result.position_clip = mul(position_instance, world_view_proj);
	//result.position_scaled_untranslated = float4( mul(position_instance.xyz, (float3x3) local_matrix), 1);

	// this is not precisely in local frame, can cause trouble with big worlds
	result.position_scaled_untranslated.xyz = mul(position_instance.xyz, (float3x3) local_matrix);
	result.position_scaled_untranslated.xyz += object_.voxel_offset;
	result.position_scaled_untranslated.w = 1;

	result.texcoord0 = __texcoord0;
	result.material_weights = __material_weights;

	result.normal_object = __normal;
	result.normal_world = mul(__normal, normal_matrix);

	result.tangent_object = __tangent;
	result.tangent_world = float4(mul(__tangent.xyz, normal_matrix), __tangent.w);

	result.key_color = object_.key_color;
	result.custom_alpha = object_.custom_alpha;

	result._local_matrix = local_matrix;
	result._view_proj_matrix = view_proj;

#ifdef PASS_OBJECT_VALUES_THROUGH_STAGES
	result.key_color = __colormask.xyz;
	result.custom_alpha = __colormask.w;
#endif

#if defined(USE_CUBE_INSTANCING) || defined(USE_DEFORMED_CUBE_INSTANCING)
	result.custom_alpha = __colormask.w * (__packed_bone7.w ? -1 : 1) + object_.custom_alpha;
#endif

	return result;
}

// TOKEN START
__MATERIAL_DECLARATIONS__
// TOKEN END

cbuffer Material : register( MERGE(b,MATERIAL_SLOT) )
{
	MaterialConstants material_;
};

// TOKEN START
__MATERIAL_VERTEXPROGRAM__
// TOKEN END
