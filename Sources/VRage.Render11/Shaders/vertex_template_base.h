#include <template.h>

/////////////////////////////////////////////////////

struct VertexShaderInterface {
	// inout
	float4 position_local;
	float4 position_clip;

	float2 texcoord0;
	float3 material_weights;
	float  ambient_occlusion;

	float3 normal_object;
	float3 normal_world;

	float4 tangent_object;
	float4 tangent_world;

	float3 key_color;
	float custom_alpha;


	// in
	float morphing;
	float4 position_scaled_untranslated; // for triplanar mapping + and foliage!!! (translation causes artifacts due lerp in rasterizer, pixel shader will do the translation)
	float4 position_scaled_translated; // for foliage
	matrix _local_matrix;
	float3 cDir;
	float3 cPos;
	float3 lDir;
	matrix _view_proj_matrix;
	float3 view_indices;
	float3 view_blends;
	float3 view_indices_light;
	float3 view_blends_light;

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
	data.ambient_occlusion = 0;
	data.normal_object = 0;
	data.normal_world = 0;
	data.tangent_object = 0;
	data.tangent_world = 0;
	data.key_color = 0;
	data.custom_alpha = 0;
	data.morphing = 0;
	data.cDir = 0;
	data.cPos = 0;
	data.lDir = 0;
	data.view_indices = 0;
	data.view_blends = 0;
	data.view_indices_light = 0;
	data.view_blends_light = 0;

	return data;
}


#include <vertex_transformations.h>
#include <vertex_merge_instancing.h>
#include <alphamaskViews.h>

matrix construct_matrix_43(float4 a, float4 b, float4 c) {
	return transpose(matrix(a, b, c, float4(0,0,0,1)));
}

float3 get_translation(matrix m) {
	return m._41_42_43;
}

matrix create_world(float4 position, float3 forward, float3 up)
{
	float3 vector3_1 = normalize(-forward);
	float3 vector2 = normalize(cross(up, vector3_1));
	float3 vector3_2 = cross(vector3_1, vector2);
	matrix m = 0;
	m._11_12_13 = vector2;
	m._21_22_23 = vector3_2;
	m._31_32_33 = vector3_1;
	m._41_42_43_44 = position;
	return m;
}

// TOKEN START
__VERTEXINPUT_DECLARATIONS__
// TOKEN END

VertexShaderInterface __prepare_interface(__VertexInput input, uint sv_vertex_id = 0, uint sv_instance_id = 0) {
	//
	float4 __position_object = 0;
	float4 __color = 0;
	float3 __material_weights = 0;
	float  __ambient_occlusion = 0;
	float2 __texcoord0 = 0;
	float3 __normal = 0;
	float4 __tangent = 0;
	// morphing
	float4 __position_object_morph = 0;
	float3 __normal_morph = 0;
	float3 __material_weights_morph = 0;
	float  __ambient_occlusion_morph = 0;
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
	float2 __uvOffset = float2(0,0);

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
	normal_matrix = (float3x3)local_matrix;
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
		morphing = voxel_morphing(local_position, get_voxel_lod_range(object_.voxelLodSize), get_camera_position());
		
		//if (object_.voxelLodSize == 0)
			//morphing = 1;
//		if (object_.voxelLodSize == 1)
	//		morphing = 0;

		result.morphing = morphing;
	}
	position_object = lerp(__position_object, __position_object_morph, morphing);
	__normal = normalize(lerp(__normal, __normal_morph, morphing));
	__material_weights = lerp(__material_weights, __material_weights_morph, morphing);
	__ambient_occlusion = lerp(__ambient_occlusion, __ambient_occlusion_morph, morphing);
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

	float4 position = local_matrix._41_42_43_44;
	float4x4 faced_matrix = local_matrix;


#if defined(USE_GENERIC_INSTANCING) || defined(USE_CUBE_INSTANCING) || defined(USE_DEFORMED_CUBE_INSTANCING)
#ifdef USE_GENERIC_INSTANCING
	// Facing is defined in render code (VertexComponents.cs)
	// 0 - none
	// 1 - vertical
	// 2 - full
	// 3 - impostor
	if (object_.facing > 0)
	{
		__instance_matrix._41_42_43 += mul(object_.voxel_offset, __instance_matrix);


		if (object_.facing == 1)
		{
			__instance_matrix._41_42_43 -= __instance_matrix._21_22_23;
		}

		float4 pos_ = mul(mul(float4(0, 0, 0, 1), __instance_matrix), local_matrix);

		float3 up = __instance_matrix._21_22_23;
		position = __instance_matrix._41_42_43_44;


#ifdef DEPTH_ONLY
		float3 forward = -frame_.directionalLightVec;
#else
		float3 forward = -normalize(pos_.xyz);
#endif

		if (object_.facing == 1)
		{
			float3 right = cross(up, -forward);
			forward = cross(up, right);
		}
		
		local_matrix = __instance_matrix;
		__instance_matrix = create_world(position, forward, up);

		if (object_.facing == 3)
		{
			float3 cameraDir = mul(position_instance, __instance_matrix);

			result.lDir = normalize(mul(frame_.directionalLightVec, transpose((float3x3)local_matrix)));
			result.cPos = mul(cameraDir, transpose((float3x3)local_matrix));
			cameraDir = mul(cameraDir - position, transpose((float3x3)local_matrix));
			result.cDir = cameraDir;


			int3 vv;
			float3 rr;
			float3 fw = mul(-forward, transpose((float3x3)local_matrix));
			fw.z *= -1;
			findViews(0, fw, vv, rr);

			int3 vvLight;
			float3 rrLight;
			findViews(0, frame_.directionalLightVec, vvLight, rrLight);

			result.view_indices = vv;
			result.view_blends = rr;
			result.view_indices_light = vvLight;
			result.view_blends_light = rrLight;

			float maxDistance = 3000;
			float blendDistance = 2000;
			float distanceScale = 1 - saturate((length(pos_) - maxDistance) / blendDistance);
			{
				__instance_matrix._11_12_13 *= distanceScale;
				__instance_matrix._21_22_23 *= distanceScale;
				__instance_matrix._31_32_33 *= distanceScale;
			}
		}
	}
#endif

	//if (object_.facing == 0)
		//position_instance.x += 0.125 * sin(frame_.time * 10 + position.x + position_instance.x) * __normal.x;
		//position_instance.y += 0.125 * sin(frame_.time * 10 + position.x + position_instance.x) * __normal.y;
		//position_instance.x += 0.8 * sin(frame_.time * 10 + position.x + position_instance.x) * __normal.x;
		//position_instance.xyz += 0.1 * sin(frame_.time  + position.x + position_instance.x) * __normal;

	position_instance = mul(position_instance, __instance_matrix);
	normal_matrix = mul((float3x3)__instance_matrix, normal_matrix);
	//normal_matrix = (float3x3)__instance_matrix;;
#else

if (object_.facing > 0)
{

#ifdef DEPTH_ONLY
	float3 forward = frame_.directionalLightVec;
#else
	float3 forward = -normalize(position.xyz);
#endif

	float3 up = local_matrix._21_22_23;
	position += mul(object_.voxel_offset, local_matrix);
	
	if (object_.facing == 1) //vertical
	{
		float3 right = cross(up, -forward);
		forward = cross(up, right);
	}

	faced_matrix = create_world(position, forward, up);
	normal_matrix = (float3x3)faced_matrix;
	

	if (object_.facing == 3) //impostor
	{
		float3 right = faced_matrix._11_12_13;

		up = faced_matrix._21_22_23;

		float3 cameraDir = mul(position_instance, faced_matrix);

		result.lDir = normalize(mul(frame_.directionalLightVec, transpose((float3x3)local_matrix)));
		result.cPos = mul(cameraDir, transpose((float3x3)local_matrix));
		cameraDir = mul((cameraDir - position), transpose((float3x3)local_matrix));
		result.cDir = cameraDir;


		int3 vv;
		float3 rr;
		float3 fw = mul(-forward, transpose((float3x3)local_matrix));
		fw.z *= -1;
		findViews(0, fw, vv, rr);

		int3 vvLight;
	    float3 rrLight;
		findViews(0, frame_.directionalLightVec, vvLight, rrLight);

		result.view_indices = vv;
		result.view_blends = rr;
		result.view_indices_light = vvLight;
		result.view_blends_light = rrLight;
	}

}
#endif

	// FINAL TRANSFORMATIONS
	// position instance is position in local space
	// local and world space are the same for shaders

if (object_.windScaleAndFreq.x > 0)
{
	position_instance.xyz += object_.windScaleAndFreq.x * sin(frame_.time * object_.windScaleAndFreq.y + position.x + position_instance.x + object_.voxel_offset) * __normal;
	//position_instance.xyz += object_.voxel_scale.x / 3 * sin(frame_.time * object_.voxel_scale.y / 8  + position.x + position_instance.x + object_.voxel_offset) * __normal;
	//position_instance.xyz += 0.5 *  __normal;
	//position_instance.xyz -= 0.05 *  __normal;
}

	result.position_local = mul(position_instance, faced_matrix);

	if (object_.massive_center_radius.w > 0)
	{
		float3 wc = object_.massive_center_radius.xyz;

		float3 delta = result.position_local.xyz - wc;
		float3 fullSphere = normalize(delta) * object_.massive_center_radius.w;

		float distance = length(result.position_local);
		float3 roundedDelta = lerp(delta, fullSphere, clamp((distance - 30000) / 50000.0f, 0, 0.8f));

		result.position_local.xyz = wc + roundedDelta;
	}

	result.position_clip = mul(result.position_local, view_proj);

	// Passing translated version to rasterizer can cause trouble with big worlds
	result.position_scaled_untranslated.xyz = position_instance.xyz * object_.voxel_scale; // , (float3x3)local_matrix) + object_.voxel_offset;//mul(position_instance.xyz, (float3x3) local_matrix);
	result.position_scaled_untranslated.w = 1;
	result.position_scaled_translated = result.position_scaled_untranslated;
	result.position_scaled_translated.xyz += object_.voxel_offset;

	result.texcoord0 = __texcoord0;

	result.material_weights = __material_weights;
	result.ambient_occlusion = __ambient_occlusion;

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
	result.custom_alpha += __colormask.w;
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
