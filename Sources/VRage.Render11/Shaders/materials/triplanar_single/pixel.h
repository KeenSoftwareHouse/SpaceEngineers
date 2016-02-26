#include <common.h>
#include <Math/math.h>
#include <triplanar_sampling.h>
#include <frame.h>
#include <voxel_ambient_occlusion.h>
#include <voxel_transition.h>


void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	ProcessDithering(pixel, output);

	if (output.DISCARD == 1)
		return;

#ifndef DEPTH_ONLY

	float d = pixel.custom.distance;
	float3 N = normalize(pixel.custom.normal);
	float3 weights = saturate(triplanar_weights(N));

	weights /= dot(weights, 1);

	float4 color_metal = 0;
	float3 normal = 0;
	float gloss = 0;

	float4 das = GetNearestDistanceAndScale(d, material_.distance_and_scale);


	float scale_near = das.z;
	float scale_far = das.w;

	float distance_near = das.x;
	float distance_far = das.y;

	uint texture_near = 0;
	uint texture_far = 0;

    float voxelLodSize = 0;
    float3 voxelOffset = 0;

#ifdef USE_VOXEL_DATA
    voxelLodSize = object_.voxelLodSize;
    voxelOffset = object_.voxel_offset;
#endif

	float pixelizationDistance = 10;
	
	float pixelizationMultiplier_near = step(pixelizationDistance, das.x);
	float pixelizationMultiplier_far = step(pixelizationDistance, das.y);

    float3 offset_near = pixelizationMultiplier_near * voxelOffset;
    float3 offset_far = pixelizationMultiplier_far * voxelOffset;

	float3 texcoords_near = pixel.custom.texcoords + offset_near;
	float3 texcoords_far = pixel.custom.texcoords + offset_far;

	float2 texcoords_ddx[3];
	float2 texcoords_ddy[3];
	calc_derivatives(pixel.custom.texcoords, texcoords_ddx, texcoords_ddy);

	if (material_.distance_and_scale_far.y > 0)
	{
		if (distance_near >= material_.distance_and_scale_far.y)
		{
			scale_near = material_.distance_and_scale_far.x;
			texture_near = 1;
		}
		if (distance_far >= material_.distance_and_scale_far.y)
		{
			scale_far = material_.distance_and_scale_far.x;
			texture_far = 1;
		}
	}

	if (material_.distance_and_scale_far.w > 0)
	{
		if (distance_near >= material_.distance_and_scale_far.w)
		{
			scale_near = material_.distance_and_scale_far.z;
			texture_near = 2;
		}
		if (distance_far >= material_.distance_and_scale_far.w)
		{
			scale_far = material_.distance_and_scale_far.z;
			texture_far = 2;
		}
	}

	if (material_.distance_and_scale_far3.y > 0)
	{
		if (distance_near >= material_.distance_and_scale_far3.y)
		{
			scale_near = material_.distance_and_scale_far3.x;
			texture_near = 3;
		}
		if (distance_far >= material_.distance_and_scale_far3.y)
		{
			scale_far = material_.distance_and_scale_far3.x;
			texture_far = 3;
		}
	}


	float scale_weight = saturate(saturate((d - distance_near) / (distance_far - distance_near) - 0.25f) * 1.5f);

	scale_near = 1.0f / scale_near;
	scale_far = 1.0f / scale_far;

	float4 color_near = float4(0, 0, 0, 0);
	float4 normal_gloss_near = float4(0, 0, 0, 0);
	float4 ext_near = float4(0, 0, 0, 0);

	float4 color_far = float4(0, 0, 0, 0);
	float4 normal_gloss_far = float4(0, 0, 0, 0);
	float4 ext_far = float4(0, 0, 0, 0);

	if (texture_near == 0)
	{
		color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
	}
	else
		if (texture_near == 1)
		{
			color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
			normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
			ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
		}
		else
			if (texture_near == 2)
			{
				color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
				normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
				ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
			}
			else
				if (texture_near == 3)
				{
					color_near = float4(material_.color_far3.xyz, 0);
					normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
					ext_near = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_near);
				}

	if (texture_far == 0)
	{
		color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
	}
	else
		if (texture_far == 1)
		{
			color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
			normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
			ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
		}
		else
			if (texture_far == 2)
			{
				color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
				normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
				ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
			}
			else
				if (texture_far == 3)
				{
					color_far = float4(material_.color_far3.xyz, 0);
					normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
					ext_far = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, scale_far);
				}



	float highPass = 1;

	if (material_.extension_detail_scale > 0)
	{
		float4 highPass1 = 1;
		float4 highPass2 = 1;

		if (pixelizationMultiplier_near > 0)
		{
			if (texture_near == 0)
			{
				highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[0], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
			}
			else
				if (texture_near == 1)
				{
					highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[1], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
				}
				else
					if (texture_near == 2)
					{
						highPass1 = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_near, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
					}
		}
	
		if (pixelizationMultiplier_far > 0)
		{
			if (texture_far == 0)
			{
				highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[0], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
			}
			else
				if (texture_far == 1)
				{
					highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[1], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
				}
				else
					if (texture_far == 2)
					{
						highPass2 = sample_color_triplanar_grad(Ext_BottomSides_Up[2], texcoords_far, weights, N, texcoords_ddx, texcoords_ddy, material_.extension_detail_scale);
					}
		}

		highPass = lerp(highPass1.z, highPass2.z, scale_weight);
	}

	//x = AO
	//y = emissivity
	//z = lowFreq noise
	//a = alpha mask

	float4 ext = lerp(ext_near, ext_far, scale_weight);

	//color_metal += lerp(color_near, color_far, scale_weight);
	color_metal += highPass * lerp(color_near, color_far, scale_weight);
	//color_metal.xyz += material_.color_far3.xyz;
	//color_metal += float4(N, 1);
	//color_metal = float4(ext.x, ext.x, ext.x, 1);
	//color_metal = highPass;
	normal += normalize(lerp(normal_gloss_near.xyz, normal_gloss_far.xyz, scale_weight) );
	gloss += lerp(normal_gloss_near.w, normal_gloss_far.w, scale_weight);

	output.base_color = color_metal.xyz;

	//#ifdef DEBUG
		if (frame_.debug_voxel_lod == 1.0f)
		{
            float3 debugColor = DEBUG_COLORS[clamp(voxelLodSize, 0, 15)];
			output.base_color.xyz = debugColor;
			//output.base_color.xyz = pixel.custom.distance;
		}
	//#endif

	//output.base_color = ext.x*ext.x*ext.x; // color_metal.xyz;
	output.metalness = color_metal.w;

	output.normal = normalize(mul(normal.xyz, pixel.custom.world_matrix));
	output.gloss = gloss;
	output.emissive = ext.y; 


	if (pixel.custom.dark_side > 0.0)
	{
		output.id = 4;
		// ambient
		output.ao = 1 - pixel.custom.dark_side;
		output.ao = output.ao * output.ao * output.ao * output.ao * ext.x * ext.x * ext.x;
	}
	else
	{
		output.id = 0;
		// ambient
		output.ao = ext.x * ext.x * ext.x;
	}

	float hardAmbient = 1-pixel.custom.ambient_occlusion;

	output.base_color *= hardAmbient;
	output.metalness *= hardAmbient;
	output.gloss *= hardAmbient;
#endif
}
