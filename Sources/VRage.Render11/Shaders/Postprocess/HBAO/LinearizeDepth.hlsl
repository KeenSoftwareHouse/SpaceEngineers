/*
#permutation RESOLVE_DEPTH 0 1
*/

/* 
* Copyright (c) 2008-2016, NVIDIA CORPORATION. All rights reserved. 
* 
* NVIDIA CORPORATION and its licensors retain all intellectual property 
* and proprietary rights in and to this software, related documentation 
* and any modifications thereto. Any use, reproduction, disclosure or 
* distribution of this software and related documentation without an express 
* license agreement from NVIDIA CORPORATION is strictly prohibited. 
*/

#include "FullScreenTriangle_Common.hlsli"
#include "ConstantBuffers.hlsli"

Texture2D<float> DepthTexture : register(t0);

//----------------------------------------------------------------------------------
float ConvertToViewDepth(float HardwareDepth)
{
    float NormalizedDepth = saturate(g_fInverseDepthRangeA * HardwareDepth + g_fInverseDepthRangeB);

    //return g_fLinearizeDepthB / (HardwareDepth + g_fLinearizeDepthA);
    return 1.0 / (NormalizedDepth * g_fLinearizeDepthA + g_fLinearizeDepthB);
}

//----------------------------------------------------------------------------------
float __pixel_shader(PostprocessVertex IN) : SV_TARGET
{
    AddViewportOrigin(IN);

    float HardwareDepth = DepthTexture.Load(int3(IN.position.xy, 0));

    return ConvertToViewDepth(HardwareDepth);
}
