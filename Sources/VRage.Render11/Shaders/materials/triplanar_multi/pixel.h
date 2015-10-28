#include <common.h>
#include <math.h>
#include <triplanar_sampling.h>
#include <voxel_ambient_occlusion.h>

float4 GetNearestDistanceAndScale(float distance, float4 materialSettings)
{
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


void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifndef DEPTH_ONLY
	float3 mat_weights = pixel.custom.mat_weights;

	float d = pixel.custom.distance;
	float3 N = normalize(pixel.custom.normal);
	float3 weights = saturate(triplanar_weights(N));
	
	weights /= dot(weights, 1);


	float4 color_metal = 0;
	float3 normal = 0;
	float gloss = 0;
	float4 ext = 0;


	[unroll]
	for (uint t = 0; t < 3; t++)
	{
		[branch]
		if (t == 2 && mat_weights[2] == 0)
		{
			break;
		}

		float4 das = GetNearestDistanceAndScale(d, material_.distance_and_scale[t]);

		float scale_near = das.z;
		float scale_far = das.w;

		float distance_near = das.x;
		float distance_far = das.y;

		int texture_near = 0;
		int texture_far = 0;

		float pixelizationDistance = 10;

		float pixelizationMultiplier_near = step(pixelizationDistance, das.x);
		float pixelizationMultiplier_far = step(pixelizationDistance, das.y);


		if (material_.distance_and_scale_far[t].y > 0)
		{
			if (distance_near >= material_.distance_and_scale_far[t].y)
			{
				scale_near = material_.distance_and_scale_far[t].x;
				texture_near = 1;
			}
			if (distance_far >= material_.distance_and_scale_far[t].y)
			{
				scale_far = material_.distance_and_scale_far[t].x;
				texture_far = 1;
			}
		}

		if (material_.distance_and_scale_far[t].w > 0)
		{
			if (distance_near >= material_.distance_and_scale_far[t].w)
			{
				scale_near = material_.distance_and_scale_far[t].z;
				texture_near = 2;
			}
			if (distance_far >= material_.distance_and_scale_far[t].w)
			{
				scale_far = material_.distance_and_scale_far[t].z;
				texture_far = 2;
			}
		}




		float scale_weight = saturate(saturate((d - distance_near) / (distance_far - distance_near) - 0.25f) * 1.5f);

		scale_near = 1.0f / scale_near;
		scale_far = 1.0f / scale_far;

		float3 offset_near = pixelizationMultiplier_near * object_.voxel_offset;
		float3 offset_far = pixelizationMultiplier_far * object_.voxel_offset;

		float3 texcoords_near = pixel.custom.texcoords + offset_near;
		float3 texcoords_far = pixel.custom.texcoords + offset_far;

	

		float2 texcoords_ddx[3];
		float2 texcoords_ddy[3];
		calc_derivatives(texcoords_far, texcoords_ddx, texcoords_ddy);

		float4 color_near = float4(0, 0, 0, 0);
		float4 normal_gloss_near = float4(0, 0, 0, 0);

		float4 color_far = float4(0, 0, 0, 0);
		float4 normal_gloss_far = float4(0, 0, 0, 0);

		float4 ext_far = float4(0, 0, 0, 0);
		float4 ext_near = float4(0, 0, 0, 0);
		
		if (texture_near == 0)
		{
			color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
			normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
			ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
		}
		else
			if (texture_near == 1)
			{
				color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
				normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
				ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
			}
			else
				if (texture_near == 2)
				{
					color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
					normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
					ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near) *  mat_weights[t];
				}

		if (texture_far == 0)
		{
			color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
			normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
			ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
		}
		else
			if (texture_far == 1)
			{
				color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
				normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
				ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
			}
			else
				if (texture_far == 2)
				{
					color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * t + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
					normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * t + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
					ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far) *  mat_weights[t];
				}

		float highPass = 1;
		if (material_.extension_detail_scale[t] > 0)
		{
			float4 highPass1 = 1;
			float4 highPass2 = 1;

			if (pixelizationMultiplier_near > 0)
			{
				if (texture_near == 0)
				{
					highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
				}
				else
					if (texture_near == 1)
					{
						highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
					}
					else
						if (texture_near == 2)
						{
							highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
						}
			}

			if (pixelizationMultiplier_far > 0)
			{
				if (texture_far == 0)
				{
					highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
				}
				else
					if (texture_far == 1)
					{
						highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
					}
					else
						if (texture_far == 2)
						{
							highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[3 * t + 2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
						}
			}

			highPass = lerp(highPass1.z, highPass2.z, scale_weight);
		}

		ext += lerp(ext_near, ext_far, scale_weight);

		color_metal += highPass * lerp(color_near, color_far, scale_weight);
		//color_metal += lerp(color_near, color_far, scale_weight);
		//color_metal += float4(highPass, highPass, highPass, 1);
		normal += lerp(normal_gloss_near.xyz, normal_gloss_far.xyz, scale_weight);
		gloss += lerp(normal_gloss_near.w, normal_gloss_far.w, scale_weight);
	}

	output.base_color = color_metal.xyz;

	if (frame_.debug_voxel_lod == 1.0f)
	{
		float3 debugColor = DEBUG_COLORS[clamp(object_.voxelLodSize, 0, 15)];
		output.base_color.xyz = debugColor;
		//output.base_color.xyz = pixel.custom.distance;
	}

	output.metalness = color_metal.w;

	output.normal = normalize(mul(normal.xyz, pixel.custom.world_matrix));
	output.gloss = gloss;
	output.emissive = ext.y;
	output.id = 4;


	// ambient
	output.ao = ext.x *ext.x*ext.x* compute_voxel_ambient_occlusion(pixel.custom.ambient_occlusion, d);
#endif
}
