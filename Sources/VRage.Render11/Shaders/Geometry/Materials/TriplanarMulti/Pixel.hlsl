#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Common.hlsli>
#include <Math/Math.hlsli>
#include <Geometry/TriplanarSampling.hlsli>
#include <Geometry/VoxelTransition.hlsli>

#define WANTS_POSITION_WS 1

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	ProcessDithering(pixel, output);

#ifndef DEPTH_ONLY

	float3 mat_weights = pixel.custom.mat_weights;

	float d = pixel.custom.distance;
	float3 N = normalize(pixel.custom.normal);
	float3 weights = saturate(triplanar_weights(N));
	
	float4 color_metal = 0;
	float4 normal_gloss = 0;
	float4 ext = 0;

    float voxelLodSize = 0;
    float3 voxelOffset = 0;

#ifdef USE_VOXEL_DATA
    voxelLodSize = object_.voxelLodSize;
    voxelOffset = object_.voxel_offset;
#endif
    float3 pos_ddx = ddx(pixel.position_ws);
    float3 pos_ddy = ddy(pixel.position_ws);
    float3 dpxperp = cross(N, pos_ddx);
    float3 dpyperp = cross(pos_ddy, N);

	float2 texcoords_ddx[3];
	float2 texcoords_ddy[3];
	calc_derivatives(pixel.custom.texcoords, texcoords_ddx, texcoords_ddy);

    [unroll]
	for (uint t = 0; t < 3; t++)
	{
		[branch]
		if (t == 2 && mat_weights[2] == 0)
		{
			break;
		}

        TriplanarMaterialConstants material;
        material.distance_and_scale = material_.distance_and_scale[t];
        material.distance_and_scale_far = material_.distance_and_scale_far[t];
        material.distance_and_scale_far3 = material_.distance_and_scale_far3[t].xy;
        material.extension_detail_scale = material_.extension_detail_scale[t];
        material.color_far3 = material_.color_far3[t];
        
        TriplanarOutput triplanarOutput;
        SampleTriplanar(3 * t, material, d, N, weights, voxelOffset, dpxperp, dpyperp, pixel.custom.texcoords, texcoords_ddx, texcoords_ddy, triplanarOutput);
        
		color_metal += triplanarOutput.color_metal * mat_weights[t];
	    normal_gloss += triplanarOutput.normal_gloss * mat_weights[t];
        ext += triplanarOutput.ext * mat_weights[t];
    }
    //color_metal.xyz = mat_weights;

	output.base_color = color_metal.xyz;
	if (frame_.debug_voxel_lod == 1.0f)
	{
		float3 debugColor = DEBUG_COLORS[clamp(voxelLodSize, 0, 15)];
		output.base_color.xyz = debugColor;
	}

	output.metalness = color_metal.w;
    output.normal = normalize(mul(normal_gloss.xyz, pixel.custom.world_matrix));
	output.gloss = normal_gloss.w;
	output.emissive = ext.y;

    output.ao = ext.x;

    float hardAmbient = 1-pixel.custom.ambient_occlusion;
	output.base_color *= hardAmbient;
#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
