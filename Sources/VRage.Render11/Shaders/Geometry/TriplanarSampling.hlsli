#include <Geometry/Materials/TriplanarMaterialConstants.hlsli>

#define INVERT_NM_Y
#define DEBUG_TEX_COORDS 0

float4 sample_color_triplanar_grad(Texture2DArray<float4> tex, int sliceIndexXZnY, int sliceIndexY,
	float3 texcoords, float3 weights, float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f)
{
	float2 texcoords_x = texcoords.zy * f;
	float2 texcoords_y = texcoords.xz * f;
	float2 texcoords_z = texcoords.xy * f;

	float4 result =
		tex.SampleGrad(TextureSampler, float3(texcoords_x, sliceIndexXZnY), texcoords_ddx[0] * f, texcoords_ddy[0] * f) * weights.x +
		tex.SampleGrad(TextureSampler, float3(texcoords_y, sliceIndexY), texcoords_ddx[1] * f, texcoords_ddy[1] * f) * weights.y +
		tex.SampleGrad(TextureSampler, float3(texcoords_z, sliceIndexXZnY), texcoords_ddx[2] * f, texcoords_ddy[2] * f) * weights.z;

#ifdef DEBUG
#if DEBUG_TEX_COORDS
	result.rgb = texcoords;
#endif
#endif

	return result;
}

float3x3 PixelTangentSpaceX(float3 N, float3 dpxperp, float3 dpyperp, float2 uv_ddx, float2 uv_ddy)
{
    float3 T = dpyperp * uv_ddx.x + dpxperp * uv_ddy.x;
    float3 B = dpyperp * uv_ddx.y + dpxperp * uv_ddy.y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

//#define SIMPLE_NORMALMAPPING 1

float4 sample_normal_gloss_triplanar(Texture2DArray<float4> tex, int sliceIndexXZnY, int sliceIndexY,
	float3 texcoords, float3 weights, float3 N, float2 texcoords_ddx[3], float2 texcoords_ddy[3], float f, float3 dpxperp, float3 dpyperp)
{
	float2 texcoords_x = texcoords.zy * f;
	float2 texcoords_y = texcoords.xz * f;
	float2 texcoords_z = texcoords.xy * f;

    float4 nm_gloss_x = tex.SampleGrad(TextureSampler, float3(texcoords_x, sliceIndexXZnY), texcoords_ddx[0] * f, texcoords_ddy[0] * f);
    float4 nm_gloss_y = tex.SampleGrad(TextureSampler, float3(texcoords_y, sliceIndexY), texcoords_ddx[1] * f, texcoords_ddy[1] * f);
    float4 nm_gloss_z = tex.SampleGrad(TextureSampler, float3(texcoords_z, sliceIndexXZnY), texcoords_ddx[2] * f, texcoords_ddy[2] * f);

	float gloss = dot(float3(nm_gloss_x.w, nm_gloss_y.w, nm_gloss_z.w), weights);

	float3 nx = nm_gloss_x.xyz * 2 - 1;
	float3 ny = nm_gloss_y.xyz * 2 - 1;
	float3 nz = nm_gloss_z.xyz * 2 - 1;
    nx.x = -nx.x;
    ny.x = -ny.x;
    nz.x = -nz.x;

#ifdef SIMPLE_NORMALMAPPING
    nx = float3(nx.z, nx.y, nx.x);
    nx.x *= sign(N.x);
    ny = float3(ny.x, ny.z, ny.y); 
    ny.y *= sign(N.y);
    nz.z *= sign(N.z);
#else
    nx = mul(nx, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[0] * f, texcoords_ddy[0] * f));
    ny = mul(ny, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[1] * f, texcoords_ddy[1] * f));
    nz = mul(nz, PixelTangentSpaceX(N, dpxperp, dpyperp, texcoords_ddx[2] * f, texcoords_ddy[2] * f));
#endif
	float3 Nt = nx * weights.x + ny * weights.y + nz * weights.z;

	return float4(Nt, gloss);
}

void calc_derivatives(float3 texcoords, out float2 t_dx[3], out float2 t_dy[3])
{
	float2 texcoords_x = texcoords.zy;
	float2 texcoords_y = texcoords.xz;
	float2 texcoords_z = texcoords.xy;

	t_dx[0] = ddx(texcoords_x);
	t_dy[0] = ddy(texcoords_x);
	t_dx[1] = ddx(texcoords_y);
	t_dy[1] = ddy(texcoords_y);
	t_dx[2] = ddx(texcoords_z);
	t_dy[2] = ddy(texcoords_z);
}

struct TriplanarOutput
{
	float4 ext;
	float4 color_metal;
	float4 normal_gloss;
};

float4 GetNearestDistanceAndScale(float distance, float4 materialSettings)
{
	//float curDistance = 0;
	//float curScale = materialSettings.x;

	//float nextDistance = materialSettings.y;
	//float nextScale = materialSettings.z;

	//float4 output = float4(curDistance, nextDistance, curScale, nextScale);
	//float2 step = float2(materialSettings.w, materialSettings.z);

	//while (output.y < distance)
	//{
	//	output.xz = output.yw;
	//	output.yw *= step;
	//}
	//return output;

	float curDistance = 0;
	float curScale = materialSettings.x;

	float nextDistance = materialSettings.y;
	float nextScale = materialSettings.z;
	
	while (nextDistance < distance)
	{
		curDistance = nextDistance;
		curScale = nextScale;

		nextDistance *= materialSettings.w;
		nextScale *= materialSettings.z;
	}
	return float4(curDistance, nextDistance, curScale, nextScale);
}

struct SlicesNum
{
	int sliceColorMetalXZnY;
	int sliceColorMetalY;
	int sliceNormalGlossXZnY;
	int sliceNormalGlossY;
	int sliceExtXZnY;
	int sliceExtY;
};

SlicesNum GetSlices(TriplanarMaterialConstants material, int nDistance)
{
	SlicesNum slices;
	slices.sliceColorMetalXZnY = material.slices[nDistance].slices1.x;
	slices.sliceColorMetalY = material.slices[nDistance].slices1.y;
	slices.sliceNormalGlossXZnY = material.slices[nDistance].slices1.z;
	slices.sliceNormalGlossY = material.slices[nDistance].slices1.w;
	slices.sliceExtXZnY = material.slices[nDistance].slices2.x;
	slices.sliceExtY = material.slices[nDistance].slices2.y;
	return slices;
}

void SampleTriplanar(int startIndex, TriplanarMaterialConstants material, float d, float3 N, float3 weights, float3 voxelOffset,
	float3 dpxperp, float3 dpyperp, float3 texcoords, float2 texcoords_ddx[3], float2 texcoords_ddy[3], out TriplanarOutput output)
{
	float4 das = GetNearestDistanceAndScale(d, material.distance_and_scale);

	float distance_near = das.x;
	float distance_far = das.y;

	float scale_near = das.z;
	float scale_far = das.w;

	float texture_near = 0;
	float texture_far = 0;
	
	float pixelizationDistance = 10;

	// applies offset and .. when texture threshold distance is further then 10 meters
	float pixelizationMultiplier_near = step(pixelizationDistance, distance_near);
	float pixelizationMultiplier_far = step(pixelizationDistance, distance_far);
	
	if (material.distance_and_scale_far.y > 0)
	{
		if (distance_near >= material.distance_and_scale_far.y)
		{
			scale_near = material.distance_and_scale_far.x;
			texture_near = material.distance_and_scale_far.z;
		}
		if (distance_far >= material.distance_and_scale_far.y)
		{
			scale_far = material.distance_and_scale_far.x;
			texture_far = material.distance_and_scale_far.z;
		}
	}

	if (material.distance_and_scale_far2.y > 0)
	{
		if (distance_near >= material.distance_and_scale_far2.y)
		{
			scale_near = material.distance_and_scale_far2.x;
			texture_near = material.distance_and_scale_far2.z;
		}
		if (distance_far >= material.distance_and_scale_far2.y)
		{
			scale_far = material.distance_and_scale_far2.x;
			texture_far = material.distance_and_scale_far2.z;
		}
	}

	if (material.distance_and_scale_far3.y > 0)
	{
		if (distance_near >= material.distance_and_scale_far3.y)
		{
			scale_near = material.distance_and_scale_far3.x;
			texture_near = material.distance_and_scale_far3.z;
		}
		if (distance_far >= material.distance_and_scale_far3.y)
		{
			scale_far = material.distance_and_scale_far3.x;
			texture_far = material.distance_and_scale_far3.z;
		}
	}

	float scale_weight = saturate(((d - distance_near) / (distance_far - distance_near) - 0.5f) * 2.0f);

	SlicesNum slices_near = GetSlices(material, min(2, texture_near));
	SlicesNum slices_far = GetSlices(material, min(2, texture_far));

	scale_near = 1.0f / scale_near;
	scale_far = 1.0f / scale_far;

	float3 offset_near = pixelizationMultiplier_near * voxelOffset;
	float3 offset_far = pixelizationMultiplier_far * voxelOffset;

	float3 texcoords_near = (texcoords + offset_near);
	float3 texcoords_far = (texcoords + offset_far);

	float4 color_near = float4(0, 0, 0, 0);
	float4 normal_gloss_near = float4(0, 0, 0, 0);
	float4 ext_near = float4(0, 0, 0, 0);

	float4 color_far = float4(0, 0, 0, 0);
	float4 normal_gloss_far = float4(0, 0, 0, 0);
	float4 ext_far = float4(0, 0, 0, 0);

	color_near = sample_color_triplanar_grad(ColorMetal, slices_near.sliceColorMetalXZnY, slices_near.sliceColorMetalY,
		texcoords_near, weights, texcoords_ddx, texcoords_ddy, scale_near);
	normal_gloss_near = sample_normal_gloss_triplanar(NormalGloss, slices_near.sliceNormalGlossXZnY, slices_near.sliceNormalGlossY,
		texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near, dpxperp, dpyperp);
	ext_near = sample_color_triplanar_grad(Ext, slices_near.sliceExtXZnY, slices_near.sliceExtY,
		texcoords_near, weights, texcoords_ddx, texcoords_ddy, scale_near);
	if (texture_near == 3)
		color_near = float4(material.color_far3.xyz, 0);

	color_far = sample_color_triplanar_grad(ColorMetal, slices_far.sliceColorMetalXZnY, slices_far.sliceColorMetalY,
		texcoords_far, weights, texcoords_ddx, texcoords_ddy, scale_far);
	normal_gloss_far = sample_normal_gloss_triplanar(NormalGloss, slices_far.sliceNormalGlossXZnY, slices_far.sliceNormalGlossY,
		texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far, dpxperp, dpyperp);
	ext_far = sample_color_triplanar_grad(Ext, slices_far.sliceExtXZnY, slices_far.sliceExtY,
		texcoords_far, weights, texcoords_ddx, texcoords_ddy, scale_far);
	if (texture_far == 3)
		color_far = float4(material.color_far3.xyz, 0);

	float highPass = 1;

	if (material.extension_detail_scale > 0)
	{
		float4 highPass1 = 1;
			float4 highPass2 = 1;

			if (pixelizationMultiplier_near > 0)
			{
				highPass1 = sample_color_triplanar_grad(Ext, slices_near.sliceExtXZnY, slices_near.sliceExtY,
					texcoords_near, weights, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
				if (texture_near >= 3)
					highPass1 = 1; // use default value;
			}

		if (pixelizationMultiplier_far > 0)
		{
			highPass2 = sample_color_triplanar_grad(Ext, slices_far.sliceExtXZnY, slices_far.sliceExtY,
				texcoords_far, weights, texcoords_ddx, texcoords_ddy, material.extension_detail_scale);
			if (texture_far >= 3)
				highPass2 = 1; // use default value;
		}

		highPass = lerp(highPass1.z, highPass2.z, scale_weight);
	}

	//x = AO
	//y = emissivity
	//z = lowFreq noise
	//a = alpha mask
	output.ext = lerp(ext_near, ext_far, scale_weight);
	output.color_metal = lerp(color_near, color_far, scale_weight) * float4(highPass.xxx, 1);
	output.normal_gloss = lerp(normal_gloss_near, normal_gloss_far, scale_weight);
}
