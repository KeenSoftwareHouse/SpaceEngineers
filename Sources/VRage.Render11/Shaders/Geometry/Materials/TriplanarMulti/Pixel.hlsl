
// Following defines must be placed before TriplanarSampling.hlsli is included !
#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Common.hlsli>
#include <Math/Math.hlsli>
#include <Geometry/TriplanarSampling.hlsli>

#define WANTS_POSITION_WS 1

cbuffer VoxelMaterialsConstants : register (b6)
{
	TriplanarMaterialConstants VoxelMaterials[MAX_VOXEL_MATERIALS];
};

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	ProcessDithering(pixel, output);

#ifndef DEPTH_ONLY
	float3 mat_weights = pixel.custom.mat_weights;
	uint3 mat_indices = pixel.custom.mat_indices;

    TriplanarInterface triplanarInput;
    InitilizeTriplanarInterface(pixel, triplanarInput);
	
    TriplanarOutput triplanarWeighted;
    triplanarWeighted.cm = 0;
    triplanarWeighted.ng = 0;
    triplanarWeighted.ext = 0;
    for (uint t = 0; t < 3; t++)
	{
		[branch]
		if (mat_weights[t] >= 0.0005f)
		{
			TriplanarMaterialConstants material = VoxelMaterials[mat_indices[t]];

			TriplanarOutput triplanarOutput;
            SampleTriplanarBranched(t, material, triplanarInput, triplanarOutput);

			triplanarWeighted.cm += triplanarOutput.cm * mat_weights[t];
			triplanarWeighted.ng += triplanarOutput.ng * mat_weights[t];
			triplanarWeighted.ext += triplanarOutput.ext * mat_weights[t];
		}
    }
    /*triplanarWeighted.cm = float4(mat_weights, 0);
    triplanarWeighted.ng = float4(0.5f, 0.5f, 1.0f, 0);
    triplanarWeighted.ext = 1;*/
    FeedOutputTriplanar(pixel, triplanarInput, triplanarWeighted, output);
#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
