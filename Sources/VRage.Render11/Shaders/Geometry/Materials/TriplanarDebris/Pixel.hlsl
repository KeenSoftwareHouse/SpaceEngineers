#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Common.hlsli>
#include <Math/Math.hlsli>
#include <Geometry/TriplanarSampling.hlsli>
#include <Frame.hlsli>

#define WANTS_POSITION_WS 1

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	ProcessDithering(pixel, output);

#ifndef DEPTH_ONLY
    TriplanarInterface triplanarInput;
    InitilizeTriplanarInterface(pixel, triplanarInput);

    TriplanarOutput triplanarOutput; 
	TriplanarMaterialConstants material = material_.triplanarMaterial;
    SampleTriplanarBranched(0, material, triplanarInput, triplanarOutput);

    FeedOutputTriplanar(pixel, triplanarInput, triplanarOutput, output);
#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
