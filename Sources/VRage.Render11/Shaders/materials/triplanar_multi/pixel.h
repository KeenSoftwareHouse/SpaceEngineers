#include <common.h>
#include <math.h>
#include <triplanar_sampling.h>

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifndef DEPTH_ONLY
	float3 mat_weights = pixel.custom.mat_weights;

	float d = pixel.custom.distance;
	float3 N = normalize(pixel.custom.normal);
	float3 weights = saturate(triplanar_weights(N));
	float3 texcoords = pixel.custom.texcoords;
	weights /= dot(weights, 1);

	float2 texcoords_ddx[3]; 
	float2 texcoords_ddy[3];
	calc_derivatives(texcoords, texcoords_ddx, texcoords_ddy);

	float4 distances = frame_.terrain_texture_distances;
	float A = (d - distances.x) * distances.y;
	float B = (d - distances.z) * distances.w;
	#ifndef ENABLE_L3
		B = 0;
	#endif
	float3 L_w = saturate(float3( lerp(1, 0, A), min(lerp(0, 1, A), lerp(1, 0, B)), lerp(0, 1, B) ));
	
	uint val = dot((L_w > 0), uint3(1, 2, 4));
	uint from = firstbithigh(val);
	uint to = 31 - firstbithigh(reversebits(val));

	float4 color_metal = 0;
	float3 normal = 0;
	float gloss = 0;

	[unroll]
	for(uint i=0; i<3; i++)
	{
		[branch]
		if(i == 2 && mat_weights[2] == 0)
		{
			break;
		}

		float3 scales_factor = material_.scales[i].xyz;

		[branch]
		if(L_w[0])
		{
			float blend = mat_weights[i] * L_w[0];
			color_metal += sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * i + 0], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[0]) * blend;	
			float4 normal_gloss = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * i + 0], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[0]) * blend;
			normal += normal_gloss.xyz;
			gloss += normal_gloss.w;
		}
		[branch]
		if(L_w[1])
		{
			float blend = mat_weights[i] * L_w[1];
			color_metal += sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * i + 1], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[1]) * blend;	
			float4 normal_gloss = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * i + 1], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[1]) * blend;
			normal += normal_gloss.xyz;
			gloss += normal_gloss.w;
		}
		#ifdef ENABLE_L3
			[branch]
			if(L_w[2])
			{
                float blend = mat_weights[i] * L_w[2];
				[branch]
				if (any(material_.far_scales[i]))
				{
					float2 far_scales_factor = 1 / material_.far_scales[i].xy;
					far_scales_factor /= float2(2.5, 4);
					float3 far_distances = float3(0, material_.far_scales[i].x, material_.far_scales[i].y);
					far_distances.x = (1.0f / distances.w) + distances.z;

					float scale_near, scale_far;
					float distance_near, distance_far;
					[branch]
					if (d < far_distances.y)
					{
						scale_near = scales_factor[2];
						scale_far = far_scales_factor[0];

						distance_near = far_distances.x;
						distance_far = far_distances.y;
					}
					else
					{
						scale_near = far_scales_factor[0];
						scale_far = far_scales_factor[1];

						distance_near = far_distances.y;
						distance_far = far_distances.z;
					}

					float scale_weight = saturate((d - distance_near) / (distance_far - distance_near));


					float4 color_near = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scale_near) * blend;
					float4 normal_gloss_near = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scale_near) * blend;

					float4 color_far = sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scale_far) * blend;
					float4 normal_gloss_far = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scale_far) * blend;

					color_metal += lerp(color_near, color_far, scale_weight);
					normal += lerp(normal_gloss_near.xyz, normal_gloss_far.xyz, scale_weight);
					gloss += lerp(normal_gloss_near.w, normal_gloss_far.w, scale_weight);
				}
				else
				{
					color_metal += sample_color_triplanar_grad(ColorMetal_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[2]) * blend;
					float4 normal_gloss = sample_normal_gloss_triplanar_grad(NormalGloss_BottomSides_Up[3 * i + 2], texcoords, weights, N, texcoords_ddx, texcoords_ddy, scales_factor[2]) * blend;
					normal += normal_gloss.xyz;
					gloss += normal_gloss.w;
				}
			}
		#endif
	}
	

	output.base_color = color_metal.xyz;
	output.metalness = color_metal.w;

	output.normal = normalize(normal.xyz);
	output.gloss = gloss;


	// ambient 
	const float PerVertexAmbient = -0.349896222;
	const float highAmbientStart = 2000;
	const float highAmbientFull = 2500;
	float ambientMultiplier = lerp(1.0f, 1.5f, (d - highAmbientStart) / (highAmbientFull - highAmbientStart));
	ambientMultiplier = clamp(ambientMultiplier, 1, 1.5f);
	output.ao = saturate(1 + PerVertexAmbient * ambientMultiplier);
#endif
}