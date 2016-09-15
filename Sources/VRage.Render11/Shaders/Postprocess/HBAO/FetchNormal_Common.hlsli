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
#include <VertexTransformations.hlsli>

#if FETCH_GBUFFER_NORMAL == 2
Texture2DMS<float3> FullResNormalTextureMS  : register(t1);
#else
Texture2D<float3> FullResNormalTexture      : register(t1);
#endif

//----------------------------------------------------------------------------------
float3 FetchFullResViewNormal_GBuffer(PostprocessVertex IN)
{
    float2 PackedNormal = FullResNormalTexture[IN.position.xy].xy;
    float3 ViewNormal = unpack_normals2(PackedNormal);
    ViewNormal.z = -ViewNormal.z;
    /*float3 WorldNormal = FetchFullResWorldNormal_GBuffer(IN) * g_fNormalDecodeScale + g_fNormalDecodeBias;
    float3 ViewNormal = normalize(mul(WorldNormal, (float3x3)g_f44NormalMatrix));*/
    return ViewNormal;
}
