// @defineMandatory HORIZONTAL_PASS, VERTICAL_PASS
// @defineMandatory MAX_OFFSET 5
// @defineMandatory DENSITY_FUNCTION 0, DENSITY_FUNCTION 1

#include <postprocess_base.h>
#include <frame.h>
#include <gbuffer.h>

#ifndef MAX_OFFSET
#define MAX_OFFSET 5
#endif

// 0 == Exponential
// 1 == Gaussian
#ifndef DENSITY_FUNCTION
#define DENSITY_FUNCTION 1
#endif

struct BlurConstants
{
    float WeightFactor;
    int StencilRef;
    float2 _padding;
};

cbuffer BlurConstantBuffer : register(b5)
{
    BlurConstants BlurParameters;
};

#ifndef MS_SAMPLE_COUNT
Texture2D<float4> InputTexture : register( t0 );
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> InputTexture : register( t0 );
#endif

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
#ifndef MS_SAMPLE_COUNT
    if ( BlurParameters.StencilRef != 0 && Stencil[input.position.xy].g & BlurParameters.StencilRef ) // This should be automatic with just a proper stencil state, but doesn't seem to be
        return;
#else
    if ( BlurParameters.StencilRef != 0 && Stencil.Load(input.position.xy, 0).g & BlurParameters.StencilRef ) // This should be automatic with just a proper stencil state, but doesn't seem to be
        return;
#endif
    //output = InputTexture[input.position.xy]; return;
    float4 result = 0;
    int size = MAX_OFFSET;

    [unroll]
    for ( int offsetIndex = -size; offsetIndex <= size; ++offsetIndex )
    {
#ifdef VERTICAL_PASS
        int2 texel = input.position.xy + float2(0, offsetIndex);
#else
        int2 texel = input.position.xy + float2(offsetIndex, 0);
#endif

#ifndef MS_SAMPLE_COUNT
        float4 addToResult = InputTexture[texel];
#else
        float4 addToResult = InputTexture.Load(texel, 0);
#endif

#if DENSITY_FUNCTION == 0
        addToResult *= ExponentialDensity(abs(offsetIndex), BlurParameters.WeightFactor);
#elif DENSITY_FUNCTION == 1
        addToResult *= GaussianWeight(offsetIndex, BlurParameters.WeightFactor);
#else
        addToResult *= GaussianWeight(offsetIndex, BlurParameters.WeightFactor);
#endif

        result += addToResult;
	}

    output = result;
}