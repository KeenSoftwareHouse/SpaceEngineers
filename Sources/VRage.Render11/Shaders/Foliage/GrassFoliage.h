
#include "Foliage.h"

#define MAX_GEOMETRY_VERTICES 4

float3 CalculateWindOffset(float3 position)
{
    const float3 wind_d = frame_.wind_vec;
    if ( !any(wind_d) )
        return 0;

    float4 freq = float4(1.975, 0.973, 0.375, 0.193);
    float4 x = mad(frame_.time, length(wind_d), dot(normalize(wind_d), position));
    float4 waves = smooth_triangle_wave(freq * x);

    return normalize(wind_d) * dot(waves, 0.25);
}

void SpawnBillboard(float3 position, float3 InstancePosition, float3x3 onb, float2 scale, float3 surfaceNormal, uint textureIndex, inout TriangleStream<RenderingPixelInput> triangle_stream)
{
    RenderingPixelInput vertex;
    float3 tanx = onb[0];
    float3 tany = onb[1];
    float3 N = onb[2];
    scale.x *= 0.5;

    float3 windOffset = CalculateWindOffset(InstancePosition);
    N = normalize(N + windOffset);

    vertex.normal = normalize(surfaceNormal);
    vertex.tangent = normalize(tanx);

    float3 downLeftPosition = mad(scale.x, -tanx, position);
    float3 downRightPosition = mad(scale.x, tanx, position);
    float3 upLeftPosition = mad(N, scale.y, downLeftPosition);
    float3 upRightPosition = mad(N, scale.y, downRightPosition);

    vertex.position = world_to_clip(downLeftPosition);
    vertex.texcoord = float3(0, 1, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(downRightPosition);
    vertex.texcoord = float3(1, 1, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(upLeftPosition);
    vertex.texcoord = float3(0, 0, textureIndex);
    triangle_stream.Append(vertex);

    vertex.position = world_to_clip(upRightPosition);
    vertex.texcoord = float3(1, 0, textureIndex);

    triangle_stream.Append(vertex);
    triangle_stream.RestartStrip();
}