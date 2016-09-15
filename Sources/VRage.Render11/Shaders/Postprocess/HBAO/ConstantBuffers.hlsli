/*
* Copyright (c) 2008-2016, NVIDIA CORPORATION. All rights reserved.
*
* NVIDIA CORPORATION and its licensors retain all intellectual property
* and proprietary rights in and to this software, related documentation
* and any modifications thereto. Any use, reproduction, disclosure or
* distribution of this software and related documentation without an express
* license agreement from NVIDIA CORPORATION is strictly prohibited.
*/

#ifndef CONSTANT_BUFFERS_H
#define CONSTANT_BUFFERS_H

#include "SharedDefines.hlsli"

#ifdef __cplusplus

#define CBUFFER struct
#define REGISTER(SLOT)
#define DECLARE_CONSTANT(TYPE, VARIABLE) TYPE VARIABLE
#define PAD_FLOAT  float  g_fPad
#define PAD_FLOAT2 float2 g_f2Pad
#define PAD_FLOAT3 float3 g_f3Pad

namespace GFSDK {
namespace SSAO {

#else // __cplusplus

#define CBUFFER cbuffer
#define REGISTER(SLOT) : register(##SLOT##)
#define DECLARE_CONSTANT(TYPE, VARIABLE) TYPE g_##VARIABLE
#define PAD_FLOAT
#define PAD_FLOAT2
#define PAD_FLOAT3
#pragma pack_matrix( row_major )

#endif // __cplusplus

//
// Note: The constant buffer sizes must be a multiple of 16 bytes (d3d11 requirement).
//

CBUFFER GlobalConstantBuffer REGISTER(b0)
{
    DECLARE_CONSTANT(float2, f2InvQuarterResolution);
    DECLARE_CONSTANT(float2, f2InvFullResolution);

    DECLARE_CONSTANT(float2, f2UVToViewA);
    DECLARE_CONSTANT(float2, f2UVToViewB);

    DECLARE_CONSTANT(float, fRadiusToScreen);
    DECLARE_CONSTANT(float, fR2);
    DECLARE_CONSTANT(float, fNegInvR2);
    DECLARE_CONSTANT(float, fNDotVBias);

    DECLARE_CONSTANT(float, fSmallScaleAOAmount);
    DECLARE_CONSTANT(float, fLargeScaleAOAmount);
    DECLARE_CONSTANT(float, fPowExponent);
    DECLARE_CONSTANT(int, iUnused);

    DECLARE_CONSTANT(float, fBlurViewDepth0);
    DECLARE_CONSTANT(float, fBlurViewDepth1);
    DECLARE_CONSTANT(float, fBlurSharpness0);
    DECLARE_CONSTANT(float, fBlurSharpness1);

    DECLARE_CONSTANT(float, fLinearizeDepthA);
    DECLARE_CONSTANT(float, fLinearizeDepthB);
    DECLARE_CONSTANT(float, fInverseDepthRangeA);
    DECLARE_CONSTANT(float, fInverseDepthRangeB);

    DECLARE_CONSTANT(float2, f2InputViewportTopLeft);
    DECLARE_CONSTANT(float, fViewDepthThresholdNegInv);
    DECLARE_CONSTANT(float, fViewDepthThresholdSharpness);

    DECLARE_CONSTANT(float, fBackgroundAORadiusPixels);
    DECLARE_CONSTANT(float, fForegroundAORadiusPixels);
    DECLARE_CONSTANT(int,   iDebugNormalComponent);
    PAD_FLOAT;

    // HLSLcc has a bug with float3x4 so use float4x4 instead
    DECLARE_CONSTANT(float4x4, f44NormalMatrix);
    DECLARE_CONSTANT(float, fNormalDecodeScale);
    DECLARE_CONSTANT(float, fNormalDecodeBias);
    PAD_FLOAT2;
};

// Must match the GS from Shaders_GL.cpp
struct PerPassConstantStruct
{
    float4 f4Jitter;

    float2 f2Offset;
    float fSliceIndex;
    unsigned int uSliceIndex;
};

CBUFFER PerPassConstantBuffer REGISTER(b1)
{
    DECLARE_CONSTANT(PerPassConstantStruct, PerPassConstants);
};

// When input textures are MSAA, fetch sample 0 only
#define g_iSampleIndex 0

#ifdef __cplusplus
} // namespace SSAO
} // namespace GFSDK
#endif

#endif //CONSTANT_BUFFERS_H
