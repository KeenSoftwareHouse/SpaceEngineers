// @defineMandatory NUMTHREADS 8

#include "Defines.hlsli"

float2 Texel2UV(uint2 texel)
{
    return float2(texel) / frame_.Screen.resolution;
}

float4 Sample(uint2 texel)
{
    return Source.SampleLevel(BilinearSampler, Texel2UV(texel), 0);
}

float4 SampleGBuffer2(uint2 texel)
{
    return SourceGBuffer2.SampleLevel(BilinearSampler, Texel2UV(texel), 0);
}

float SampleDepth(uint2 texel)
{
    return SourceDepth[texel].r;
}

float4 BrightPass2(uint2 texel)
{
    const int2 offsets[ 4 ] =
    {
        float2(-1, 1),
        float2(1, 1),
        float2(-1, -1),
        float2(1, -1),
    };
    const float kSmallEpsilon = 0.0001;

    float3 colorSum = 0;
    float weight = 1.0;
    [unroll]
	for(uint i = 0; i < 4; i++)
    {
        float3 linearColor = Sample(texel + offsets[i]).rgb;
	    
        float totalLuminance = calc_luminance(linearColor) * frame_.Post.BloomExposure;
        float bloomLuminance = totalLuminance - frame_.Post.BloomLumaThreshold;
	    float bloomAmount = saturate(bloomLuminance / 2.0f);
        
        float depth = SampleDepth(texel + offsets[i]);
        float linearDepth = 1 - linearize_depth(depth, frame_.Environment.projection_matrix);
        float depthFactor = -frame_.Post.BloomDepthStrength * exp(-linearDepth * frame_.Post.BloomDepthSlope) + 1;

        float emissive = SampleGBuffer2(texel + offsets[i]).b * frame_.Post.BloomEmissiveness;
        bloomAmount = max(bloomAmount * depthFactor, emissive);
        
        colorSum += linearColor;
        weight *= bloomAmount;
    }

    return float4(colorSum  / 4 * weight, 1);
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID)
{
	uint2 texel = dispatchThreadID.xy;
    uint2 srcTexel = texel * 2 + 1;

    Destination[texel] = BrightPass2(srcTexel);
}
