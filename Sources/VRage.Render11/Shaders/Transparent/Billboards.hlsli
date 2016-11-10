#pragma once

#include <Common.hlsli>
#include <Frame.hlsli>

struct BillboardData
{
    float4 Color;

    int custom_projection_id;
    float reflective;
    float AlphaSaturation;
    float AlphaCutout;

    float3 normal;
    float SoftParticleDistanceScale;
};

cbuffer CustomProjections : register (b2)
{
    matrix view_projection[32];
};

#define BILLBOARD_BUFFER_SLOT 30

StructuredBuffer<BillboardData> BillboardBuffer : register(MERGE(t, BILLBOARD_BUFFER_SLOT));

struct VsIn
{
    float3 position : POSITION;
    float2 texcoord : TEXCOORD0;
};

void CalculateVertexPosition(VsIn vertex, uint vertex_id, out float4 projPos, out uint billboard_index)
{
    billboard_index = vertex_id / 4;

    int custom_id = BillboardBuffer[billboard_index].custom_projection_id;
    if (custom_id < 0)
        projPos = mul(float4(vertex.position.xyz, 1), frame_.Environment.view_projection_matrix);
    else
        projPos = mul(float4(vertex.position.xyz, 1), view_projection[custom_id]);
}
