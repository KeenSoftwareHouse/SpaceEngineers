#ifndef FOLIAGE_H
#define FOLIAGE_H

#include <template.h>

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

struct RenderingPixelInput
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
    float3 texcoord : TEXCOORD0;
};

float3 UnpackNormal(float2 packedNormal)
{
    float2 xy = packedNormal * 255;
    float zsign = xy.y > 127;
    xy.y -= zsign * 128;
    xy /= float2(255, 127);
    xy = xy * 2 - 1;
    float z = sqrt(1 - dot(xy, xy)) * (zsign ? 1 : -1);
    return float3(xy, z).xzy;
}
#endif