/*
#permutation ENABLE_SHARPNESS_PROFILE 0 1
#permutation KERNEL_RADIUS 2 4
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

#include "ConstantBuffers.hlsli"
#include "FullScreenTriangle_Common.hlsli"
#include "Blur_Common.hlsli"

//-------------------------------------------------------------------------
float4 __pixel_shader( PostprocessVertex IN ) : SV_TARGET
{
    SubtractViewportOrigin(IN);

    float CenterDepth;
    float AO = ComputeBlur(IN, float2(0,g_f2InvFullResolution.y), CenterDepth);

    return pow(saturate(AO), g_fPowExponent);
}
