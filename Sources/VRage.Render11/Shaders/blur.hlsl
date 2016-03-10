// @defineMandatory HORIZONTAL_PASS, VERTICAL_PASS
// @defineMandatory MAX_OFFSET 5
// @defineMandatory DENSITY_FUNCTION 0, DENSITY_FUNCTION 1
// @define COPY_ON_STENCIL_FAIL
// @define DEPTH_DISCARD_THRESHOLD 0.2

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
Texture2D<float4> InputTexture : register( t5 );
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> InputTexture : register( t5 );
#endif

float4 SampleInput(int2 texel)
{
    float4 inputSample;
#ifndef MS_SAMPLE_COUNT
    inputSample = InputTexture[texel];
#else
    inputSample = InputTexture.Load(texel, 0);
#endif
    return inputSample;
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
        //output = InputTexture[input.position.xy];
        //return;
#ifndef MS_SAMPLE_COUNT
    if ( BlurParameters.StencilRef != 0 && (Stencil[input.position.xy].g & BlurParameters.StencilRef) ) // This should be automatic with just a proper stencil state, but doesn't seem to be
#else
    if ( BlurParameters.StencilRef != 0 && (Stencil.Load(input.position.xy, 0).g & BlurParameters.StencilRef) ) // This should be automatic with just a proper stencil state, but doesn't seem to be
#endif
    {
#ifdef COPY_ON_STENCIL_FAIL
        SampleInput(input.position.xy);
#endif
        return;
    }

    float4 result = 0;
    const int size = MAX_OFFSET;

#ifdef DEPTH_DISCARD_THRESHOLD
    float depthSample = DepthBuffer[input.position.xy];
    float linearDepth = -linearize_depth(depthSample, frame_.projection_matrix);
#endif

    [unroll]
    for ( int offsetIndex = -size; offsetIndex <= size; ++offsetIndex )
    {
#ifdef VERTICAL_PASS
        int2 texel = input.position.xy + float2(0, offsetIndex);
#else
        int2 texel = input.position.xy + float2(offsetIndex, 0);
#endif

        float4 addToResult = SampleInput(texel);

#ifdef DEPTH_DISCARD_THRESHOLD  // Really expensive, avoid using this flag if at all possible
        float depthSampleAtOffset = DepthBuffer[texel];
        float linearDepthAtOffset = -linearize_depth(depthSampleAtOffset, frame_.projection_matrix);
        if ( !depth_not_background(depthSampleAtOffset) || (abs(linearDepth - linearDepthAtOffset) > DEPTH_DISCARD_THRESHOLD) )
        {
            output = InputTexture[input.position.xy];
            return;
        }
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