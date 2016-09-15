/*
#permutation ENABLE_BLUR 0 1
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

Texture2DArray<float> AOTexture     : register(t0);
Texture2D<float> DepthTexture       : register(t1);
sampler PointSampler                : register(s0);

//----------------------------------------------------------------------------------
struct PSOut
{
#if ENABLE_BLUR
    float2 AOZ  : SV_TARGET;
#else
    float4 AO   : SV_TARGET;
#endif
};

//-------------------------------------------------------------------------
PSOut __pixel_shader(PostprocessVertex IN)
{
    PSOut OUT;

#if !ENABLE_BLUR
    SubtractViewportOrigin(IN);
#endif

    int2 FullResPos = int2(IN.position.xy);
    int2 Offset = FullResPos & 3;
    int SliceId = Offset.y * 4 + Offset.x;
    int2 QuarterResPos = FullResPos >> 2;

#if ENABLE_BLUR
    float AO = AOTexture.Load(int4(QuarterResPos, SliceId, 0));
    float ViewDepth = DepthTexture.Sample(PointSampler, IN.uv);
    OUT.AOZ = float2(AO, ViewDepth);
#else
    float AO = AOTexture.Load(int4(QuarterResPos, SliceId, 0));
    OUT.AO = pow(saturate(AO), g_fPowExponent);
#endif

    return OUT;
}
