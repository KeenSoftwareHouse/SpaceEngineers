#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Common.hlsli>
#include <Math/Math.hlsli>
#include <Geometry/TriplanarSampling.hlsli>
#include <Frame.hlsli>
#include <Geometry/VoxelTransition.hlsli>

#define WANTS_POSITION_WS 1

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	ProcessDithering(pixel, output);

#ifndef DEPTH_ONLY

	float d = pixel.custom.distance;
	float3 N = normalize(pixel.custom.normal);
	float3 weights = saturate(triplanar_weights(N));

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

    TriplanarMaterialConstants material;
    material.distance_and_scale = material_.distance_and_scale;
    material.distance_and_scale_far = material_.distance_and_scale_far;
    material.distance_and_scale_far3 = material_.distance_and_scale_far3;
    material.extension_detail_scale = material_.extension_detail_scale;
    material.color_far3 = material_.color_far3;
    TriplanarOutput triplanarOutput;
    SampleTriplanar(0, material, d, N, weights, voxelOffset, dpxperp, dpyperp, pixel.custom.texcoords, texcoords_ddx, texcoords_ddy, triplanarOutput);

	output.base_color = triplanarOutput.color_metal.xyz;
	if (frame_.debug_voxel_lod == 1.0f)
	{
        float3 debugColor = DEBUG_COLORS[clamp(voxelLodSize, 0, 15)];
		output.base_color.xyz = debugColor;
	}

	output.metalness = triplanarOutput.color_metal.w;
    output.normal = normalize(mul(triplanarOutput.normal_gloss.xyz, pixel.custom.world_matrix));
	output.gloss = triplanarOutput.normal_gloss.w;
	output.emissive = triplanarOutput.ext.y; 

    output.ao = triplanarOutput.ext.x;

    float hardAmbient = 1-pixel.custom.ambient_occlusion;
	output.base_color *= hardAmbient;
#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
