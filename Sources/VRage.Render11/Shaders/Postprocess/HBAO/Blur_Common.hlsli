/*
* Copyright (c) 2008-2016, NVIDIA CORPORATION. All rights reserved.
*
* NVIDIA CORPORATION and its licensors retain all intellectual property
* and proprietary rights in and to this software, related documentation
* and any modifications thereto. Any use, reproduction, disclosure or
* distribution of this software and related documentation without an express
* license agreement from NVIDIA CORPORATION is strictly prohibited.
*/

#include <Frame.hlsli>

Texture2D<float2> AODepthTexture    : register(t0);
sampler PointClampSampler           : register(s0);
sampler LinearClampSampler          : register(s1);

// Use step size = 1/2 in the inner/outer-half of the kernel
#define USE_ADAPTIVE_SAMPLING (KERNEL_RADIUS >= 4)

// 10% gain on BlurX on GK104
#define USE_MAD_OPT 1

// Offset the center depth along the tangent direction
#define USE_DEPTH_SLOPE 1

float2 PointSampleAODepth(float2 UV)
{
    return AODepthTexture.Sample(PointClampSampler, UV).xy;
}
float2 LinearSampleAODepth(float2 UV)
{
    return AODepthTexture.Sample(LinearClampSampler, UV).xy;
}

//----------------------------------------------------------------------------------
struct CenterPixelData
{
    float2 UV;
    float Depth;
    float Sharpness;
#if USE_MAD_OPT
    float Scale;
    float Bias;
#endif
};

//----------------------------------------------------------------------------------
float CrossBilateralWeight(float R, float SampleDepth, float DepthSlope, CenterPixelData Center)
{
    const float BlurSigma = ((float)KERNEL_RADIUS+1.0) * 0.5;
    const float BlurFalloff = 1.0 / (2.0*BlurSigma*BlurSigma);

#if USE_DEPTH_SLOPE
    SampleDepth -= DepthSlope * R;
#endif

#if USE_MAD_OPT
    float DeltaZ = SampleDepth * Center.Scale + Center.Bias;
#else
    float DeltaZ = (SampleDepth - Center.Depth) * Center.Sharpness;
#endif

    return exp2(-R*R*BlurFalloff - DeltaZ*DeltaZ);
}

//-------------------------------------------------------------------------
void ProcessSample(float2 AOZ,
                   float R,
                   float DepthSlope,
                   CenterPixelData Center,
                   inout float TotalAO,
                   inout float TotalW)
{
    float AO = AOZ.x;
    float Z = AOZ.y;

    float W = CrossBilateralWeight(R, Z, DepthSlope, Center);
    TotalAO += W * AO;
    TotalW += W;
}

//-------------------------------------------------------------------------
void ProcessRadius(float R0,
                   float2 DeltaUV,
                   float DepthSlope,
                   CenterPixelData Center,
                   inout float TotalAO,
                   inout float TotalW)
{
#if USE_ADAPTIVE_SAMPLING
    float R = R0;

    [unroll]
    for (; R <= KERNEL_RADIUS/2; R += 1)
    {
        float2 UV = R * DeltaUV + Center.UV;
        float2 AOZ = PointSampleAODepth(UV);
        ProcessSample(AOZ, R, DepthSlope, Center, TotalAO, TotalW);
    }

    [unroll]
    for (; R <= KERNEL_RADIUS; R += 2)
    {
        float2 UV = (R + 0.5) * DeltaUV + Center.UV;
        float2 AOZ = LinearSampleAODepth(UV);
        ProcessSample(AOZ, R, DepthSlope, Center, TotalAO, TotalW);
    }
#else
    [unroll]
    for (float R = R0; R <= KERNEL_RADIUS; R += 1)
    {
        float2 UV = R * DeltaUV + Center.UV;
        float2 AOZ = PointSampleAODepth(UV);
        ProcessSample(AOZ, R, DepthSlope, Center, TotalAO, TotalW);
    }
#endif
}

//-------------------------------------------------------------------------
#if USE_DEPTH_SLOPE
void ProcessRadius1(float2 DeltaUV,
                    CenterPixelData Center,
                    inout float TotalAO,
                    inout float TotalW)
{
    float2 AODepth = PointSampleAODepth(Center.UV + DeltaUV);
    float DepthSlope = AODepth.y - Center.Depth;

    ProcessSample(AODepth, 1, DepthSlope, Center, TotalAO, TotalW);
    ProcessRadius(2, DeltaUV, DepthSlope, Center, TotalAO, TotalW);
}
#endif

//-------------------------------------------------------------------------
float GetSharpness(float ViewDepth)
{
#if ENABLE_SHARPNESS_PROFILE
    float lerpFactor = (ViewDepth - g_fBlurViewDepth0) / (g_fBlurViewDepth1 - g_fBlurViewDepth0);
    return lerp(g_fBlurSharpness0, g_fBlurSharpness1, saturate(lerpFactor));
#else
    return g_fBlurSharpness1;
#endif
}

//-------------------------------------------------------------------------
float ComputeBlur(PostprocessVertex IN,
                  float2 DeltaUV,
                  out float CenterDepth)
{
    float2 AOZ = PointSampleAODepth(IN.uv);
    CenterDepth = AOZ.y;

    CenterPixelData Center;
    Center.UV = IN.uv;
    Center.Depth = CenterDepth;
    Center.Sharpness = GetSharpness(CenterDepth);

#if USE_MAD_OPT
    Center.Scale = Center.Sharpness;
    Center.Bias = -Center.Depth * Center.Sharpness;
#endif

    float TotalAO = AOZ.x;
    float TotalW = 1.0;

#if USE_DEPTH_SLOPE
    ProcessRadius1(DeltaUV, Center, TotalAO, TotalW);
    ProcessRadius1(-DeltaUV, Center, TotalAO, TotalW);
#else
    float DepthSlope = 0;
    ProcessRadius(1, DeltaUV, DepthSlope, Center, TotalAO, TotalW);
    ProcessRadius(1, -DeltaUV, -DepthSlope, Center, TotalAO, TotalW);
#endif

    return TotalAO / TotalW;
}
