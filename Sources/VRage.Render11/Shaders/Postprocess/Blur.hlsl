// @defineMandatory HORIZONTAL_PASS, VERTICAL_PASS
// @defineMandatory MAX_OFFSET 5
// @defineMandatory DENSITY_FUNCTION 0, DENSITY_FUNCTION 1
// @define DEPTH_DISCARD_THRESHOLD 0.2

#include "PostprocessBase.hlsli"
#include <Frame.hlsli>
#include <GBuffer/GBuffer.hlsli>

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

float SampleDepth(int2 texel)
{
    float depthSample;
#ifndef MS_SAMPLE_COUNT
    depthSample = DepthBuffer[texel];
#else
    depthSample = DepthBuffer.Load(texel, 0);
#endif
    return depthSample;
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
    float4 result = 0;
    const int size = MAX_OFFSET;

#ifdef DEPTH_DISCARD_THRESHOLD
    float depthSample = SampleDepth(input.position.xy);
    float linearDepth = compute_depth(depthSample);
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
        float depthSampleAtOffset = SampleDepth(texel);
        float linearDepthAtOffset = compute_depth(depthSampleAtOffset);
        if ( !depth_not_background(depthSampleAtOffset) || (abs(linearDepth - linearDepthAtOffset) > DEPTH_DISCARD_THRESHOLD) )
        {
            result = SampleInput(input.position.xy);
            break;
        }
#endif

#if DENSITY_FUNCTION == 0
        addToResult *= ExponentialDensity(abs(offsetIndex), BlurParameters.WeightFactor);
#elif DENSITY_FUNCTION == 1
        addToResult *= CalcGaussianWeight(offsetIndex, BlurParameters.WeightFactor);
#else
        addToResult *= CalcGaussianWeight(offsetIndex, BlurParameters.WeightFactor);
#endif

        result += addToResult;
	}

    output = result;
}