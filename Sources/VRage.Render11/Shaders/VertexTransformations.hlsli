#ifndef VERTEX_TRANSFORMATIONS_H__
#define VERTEX_TRANSFORMATIONS_H__

float4 unpack_position_and_scale(float4 position)
{
	return float4(position.xyz * position.w, 1);
}

float4 unpack_voxel_position(float4 packed)
{
	return float4(packed.xyz * 2 - 1, 1);
}

float voxel_morphing(float3 position_a, float2 bounds, float3 local_viewer)
{
	float3 diff = abs(position_a - local_viewer);
	float dist = max(diff.x, max(diff.y, diff.z));
	return saturate(((dist - bounds.x) / (bounds.y - bounds.x) - 0.35f)*2.0f);
	//return saturate((dist - bounds.x) / (bounds.y - bounds.x));
	//return 1;
}

float3 unpack_voxel_weights(float weights)
{
#if 1
/*
	Top bit unused, then 2 bits specifying material index.
	0 00 => 0 => 0 => (1,0,0)
	0 01 => 1 => 1 => (0,1,0)
	0 10 => 2 => 2 => (0,0,1)
	(i <= 0, |i-1| <= 0, 2 <= i)
*/
	float index = floor(weights * 8.0f);
	return float3(step(index, 0), step(abs(index - 1), 0), step(2, index));
#else
	uint bits = weights * 65535;
	return float3( bits >> 10, (bits >> 5) & 0x1F, bits & 0x1F) / float3(63, 31, 31);
#endif
}

float unpack_voxel_ao(float ambient_occlusion)
{
	return frac(ambient_occlusion * 8.0f);
}

// Reference VF_Packer.cs, PackNormal
/*
// there were numerical flaws in this method
float3 unpack_normal(float4 p)
{
    float zsign = p.y > 0.5f ? 1 : -1;
    if (zsign > 0) p.y -= 0.5f;
    float2 xy = 256 * (p.xz + 256 * p.yw);
        xy /= 32767;
    xy = 2 * xy - 1;
    return float3(xy.xy, zsign * sqrt(saturate(1 - dot(xy, xy))));
}*/
float3 unpack_normal(uint2 p)
{
    float zsign = -1;
    if (p.x > 32767)
    {
        zsign = 1; 
        p.x &= (1 << 15) - 1;
    }
    float3 normal = float3(((float)p.x) / 32767, ((float)p.y) / 32767, 0);
    normal.xy = 2 * normal.xy - 1;
    normal.z = zsign * sqrt(saturate(1 - dot(normal.xy, normal.xy)));
    return normal;
}

// Reference VF_Packer.cs, PackTangentSign
float4 unpack_tangent_sign(float4 p)
{
	float sign = p.w > 0.5f ? 1 : -1;
	float zsign = p.y > 0.5f ? 1 : -1;
	if(zsign > 0) p.y -= 0.5f;
	if(sign > 0) p.w -= 0.5f;
	float2 xy = 256 * (p.xz + 256 * p.yw);
	xy /= 32767;
	xy = 2 * xy - 1;
	return float4(xy.xy, zsign * sqrt(saturate(1-dot(xy, xy))), sign);
}

// Spheremap Transform, http://aras-p.info/texts/CompactNormalStorage.html
// NOTE: Assumes normal is in view space and normalized
float2 pack_normals2(float3 n)
{
    float p = sqrt(n.z * 8 + 8);
    return float2(n.xy / p + 0.5);
}

// Spheremap Transform, http://aras-p.info/texts/CompactNormalStorage.html
float3 unpack_normals2(float2 enc)
{
    float2 fenc = enc * 4 - 2;
    float f = dot(fenc, fenc);
    float g = sqrt(1 - f / 4);
    float3 n;
    n.xy = fenc*g;
    n.z = 1 - f / 2;
    return n;
}

matrix translation_rotation_matrix(float3 translation, float3 forward, float3 up)
{
	float3 right = cross(up, -forward);

	matrix M = { float4(right, 0), float4(up, 0), float4(-forward, 0), float4(translation, 1) };
	return M;
}

matrix translation_rotation_matrix(float4 packed)
{
	static const float3 PACKED_DIR[6] = { float3(0,0,-1), float3(0,0,1), float3(-1,0,0), float3(1,0,0), float3(0,1,0), float3(0,-1,0) };

	float val = packed.w;

	float3 forward = PACKED_DIR[val / 6];
	float3 up = PACKED_DIR[val - (uint)(val / 6) * 6];

	return translation_rotation_matrix(packed.xyz, forward, up);
}

matrix construct_cube_instance_matrix(float4 cube_transformation) {

	return translation_rotation_matrix(cube_transformation);
}

// ref. Vector3UByte.Denormalize(), but position here is already in range [0,1]
float3 unpack_bone(float4 position, float range)
{
	static const float eps = 0.5 / 255;
	return (position.xyz + eps - 0.5) * range * 2;
}

matrix construct_deformed_cube_instance_matrix(
	float4 packed0, float4 packed1, float4 packed2, float4 packed3,
	float4 packed4, float4 packed5, float4 packed6, float4 packed7,
	float4 cube_transformation,
	uint4 blend_indices, float4 blend_weights
	)
{
	matrix M = translation_rotation_matrix(cube_transformation);

	[branch]
	if(packed3.w)
	{
		float4 bones[9] = {
			packed0, packed1, packed2,
			packed3, packed4, packed5,
			packed6, packed7, float4(packed0.w, packed1.w, packed2.w, 0)
		};

		matrix B = { bones[blend_indices[0]], bones[blend_indices[1]], bones[blend_indices[2]], bones[blend_indices[3]] };
		float4 translation = mul(blend_weights, B);
		M._41_42_43 += unpack_bone(translation, packed4.w / 10.f * 255.f);
	}

	return M;
}

#endif
