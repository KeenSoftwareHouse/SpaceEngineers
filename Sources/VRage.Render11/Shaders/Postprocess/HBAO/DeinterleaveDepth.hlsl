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

// Do not use gather instructions on GL to support Core 3.2

#define MRT_COUNT MAX_NUM_MRTS

Texture2D<float> DepthTexture   : register(t0);
sampler PointClampSampler       : register(s0);

//----------------------------------------------------------------------------------
struct PSOutputDepthTextures
{
    float Z00 : SV_Target0;
    float Z10 : SV_Target1;
    float Z20 : SV_Target2;
    float Z30 : SV_Target3;
#if MRT_COUNT == 8
    float Z01 : SV_Target4;
    float Z11 : SV_Target5;
    float Z21 : SV_Target6;
    float Z31 : SV_Target7;
#endif
};

//----------------------------------------------------------------------------------
PSOutputDepthTextures __pixel_shader(PostprocessVertex IN)
{
    PSOutputDepthTextures OUT;

    IN.position.xy = floor(IN.position.xy) * 4.0 + (g_PerPassConstants.f2Offset + 0.5);
    IN.uv = IN.position.xy * g_f2InvFullResolution;

    // Gather sample ordering: (-,+),(+,+),(+,-),(-,-),
    float4 S0 = DepthTexture.GatherRed(PointClampSampler, IN.uv);
    float4 S1 = DepthTexture.GatherRed(PointClampSampler, IN.uv, int2(2,0));

    OUT.Z00 = S0.w;
    OUT.Z10 = S0.z;
    OUT.Z20 = S1.w;
    OUT.Z30 = S1.z;

#if MRT_COUNT == 8
    OUT.Z01 = S0.x;
    OUT.Z11 = S0.y;
    OUT.Z21 = S1.x;
    OUT.Z31 = S1.y;
#endif

    return OUT;
}
