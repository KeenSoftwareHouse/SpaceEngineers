#ifndef FOLIAGE_H
#define FOLIAGE_H

#include <Template.hlsli>

#define MAX_FOLIAGE_PER_TRIANGLE 24

struct FoliageStreamVertex
{
    float3 position : POSITION;
    float3 position_world : POSITION1;
    float3 normal : NORMAL;
    float3 weights : TEXCOORD0;
};

struct FoliageStreamGeometryOutputVertex
{
    float3 position : TEXCOORD0;
    uint NormalSeedMaterialId : TEXCOORD1;  // 16 bits for normal, 8 for seed, 8 for id
};

struct RenderingVertexInput
{
    float3 position : POSITION;
    float4 NormalSeedMaterialId : TEXCOORD0; // First two elements for normal, third for seed, fourth for id
};

struct RenderingVertexOutput
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float4 InstancePosition : TEXCOORD0;
    uint IdSeed : TEXCOORD1;    // First 8 bits for ID, last 24 for seed
};

struct RenderingPixelInput
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float3 texcoord : TEXCOORD0;
};

uint2 PackNormal(float3 normal)
{
    uint2 packedNormal = mad(0.5, normal.xz, 0.5) * uint2(255, 127);
    packedNormal.y |= (1 << 7) * (normal.y > 0);
    return packedNormal;
}

float3 UnpackNormal(float2 packedNormal)
{
    float2 xy = packedNormal * 0xFF;
    float zsign = xy.y > 127;
    xy.y -= zsign * 128;
    xy /= float2(255, 127);
    xy = mad(2, xy, -1);
    float z = sqrt(1 - dot(xy, xy)) * (zsign ? 1 : -1);
    return float3(xy, z).xzy;
}
#endif