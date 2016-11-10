
#include "Foliage.hlsli"

#define MAX_GEOMETRY_VERTICES 4

float3 CalculateWindOffset(float3 position)
{
    const float3 wind_d = frame_.Foliage.wind_vec;
    if ( !any(wind_d) )
        return 0;

    float4 freq = float4(1.975, 0.973, 0.375, 0.193);
    float4 x = mad(frame_.frameTime, length(wind_d), dot(normalize(wind_d), position));
    float4 waves = smooth_triangle_wave(freq * x);

    return normalize(wind_d) * dot(waves, 0.25);
}

void SpawnBillboard(float3 position, float3 InstancePosition, float2 scale, float3 surfaceNormal, float3 surfaceTangent, uint textureIndex, inout TriangleStream<RenderingPixelInput> triangle_stream)
{
    RenderingPixelInput vertex;
    scale.x *= 0.5;

    float3 windOffset = CalculateWindOffset(InstancePosition);
    float3 adjustedNormal = normalize(surfaceNormal + windOffset);

    vertex.normal = normalize(surfaceNormal);
    vertex.tangent = normalize(surfaceTangent);

    float3 downLeftPosition = mad(scale.x, -surfaceTangent, position);
    float3 downRightPosition = mad(scale.x, surfaceTangent, position);
    float3 upLeftPosition = mad(adjustedNormal, scale.y, downLeftPosition);
    float3 upRightPosition = mad(adjustedNormal, scale.y, downRightPosition);

    vertex.position = WorldToClip(downLeftPosition);
    vertex.texcoord = float3(0, 1, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = WorldToClip(downRightPosition);
    vertex.texcoord = float3(1, 1, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = WorldToClip(upLeftPosition);
    vertex.texcoord = float3(0, 0, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = WorldToClip(upRightPosition);
    vertex.texcoord = float3(1, 0, textureIndex);

    triangle_stream.Append(vertex);
    triangle_stream.RestartStrip();
}