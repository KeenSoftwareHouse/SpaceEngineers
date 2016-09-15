
#include "Foliage.hlsli"

#define MAX_GEOMETRY_VERTICES 13

float2 CalculateRockScale(float3 position, float2 scale)
{
    float4 freq = float4(1.975, 0.973, 0.375, 0.193);
    float4 x = float4(position * 1000, 0);
    float4 waves = smooth_triangle_wave(freq * x);

    return scale * float2(dot(waves, 0.25), dot(waves, 0.5));
}

void SpawnPebble(float3 position, float3 InstancePosition, float3x3 onb, float2 scale, uint index, float2 hammersleySample, float viewVectorLength, inout TriangleStream<RenderingPixelInput> triangle_stream)
{
    RenderingPixelInput vertex;

    scale.y *= 0.1;
    scale *= 0.75;
    scale = CalculateRockScale(InstancePosition, scale);

    bool big_stone = true;

    if ( frac(InstancePosition.x * 10000) < 0.985 )
    {
        scale *= 0.2f;
        scale = CalculateRockScale(InstancePosition, scale);
        scale = clamp(scale, 0.01, 1);
        scale.y = 0.25 * scale.x;
        big_stone = false;
    }

    if ( !big_stone && viewVectorLength > 7.5 )
    {
        return;
    }

    float3 tanx = onb[0] * scale.x;
    float3 tany = onb[1] * scale.x;
    float3 N = normalize(onb[2]);

    hammersleySample = mad(0.5f, hammersleySample, 1.0f);

    float angle = 0;
    int segments = big_stone ? 4 : 2;
    float angle_delta = 2.0f * M_PI / (segments * 2);

    float2 SinCosAngle;
    float2 SinCosBeta;

    float beta = atan(length(tanx + tany) / length(N * hammersleySample.x * scale.y * 5));
    sincos(beta, SinCosBeta.x, SinCosBeta.y);

    float3 delta = 0;

    [unroll]
    for ( int i = 0; i < segments; i++ )
    {
        sincos(angle, SinCosAngle.x, SinCosAngle.y);

        delta = tanx * hammersleySample.y * SinCosAngle.y + tany * hammersleySample.y * SinCosAngle.x;
        vertex.position = WorldToClip(position + delta);
        vertex.normal = mad(normalize(delta), SinCosBeta.y, N * SinCosBeta.x);
        vertex.tangent = normalize(mad(-tanx, SinCosAngle.x, tany * SinCosBeta.y));
        vertex.texcoord = float3(mad(SinCosAngle.yx, 0.5f, 0.5f), index);
        triangle_stream.Append(vertex);

        vertex.position = WorldToClip(mad(N, hammersleySample.x * scale.y, position));
        vertex.normal = N;
        vertex.tangent = normalize(mad(-tanx, SinCosAngle.x, tany * SinCosAngle.y));
        vertex.texcoord = float3(0.5, 0.5, index);
        triangle_stream.Append(vertex);

        angle += angle_delta;
        sincos(angle, SinCosAngle.x, SinCosAngle.y);

        delta = tanx * SinCosAngle.y * hammersleySample.x + tany * SinCosAngle.x * hammersleySample.y;
        vertex.position = WorldToClip(position + delta);
        vertex.normal = normalize(delta) * SinCosBeta.y + N * SinCosBeta.x;
        vertex.tangent = normalize(-tanx * SinCosAngle.x + tany * SinCosAngle.x);
        vertex.texcoord = float3(mad(SinCosAngle.yx, 0.5f, 0.5f), index);
        triangle_stream.Append(vertex);

        angle += angle_delta;
    }

    sincos(angle, SinCosAngle.x, SinCosAngle.y);

    delta = tanx * SinCosAngle.y * hammersleySample.y + tany * SinCosAngle.x * hammersleySample.y;
    vertex.position = WorldToClip(position + delta);
    vertex.normal = normalize(delta) * SinCosBeta.y + N * SinCosBeta.x;
    vertex.tangent = normalize(-tanx * SinCosAngle.x + tany * SinCosAngle.y);
    vertex.texcoord = float3(SinCosAngle.y * 0.5 + 0.5, SinCosAngle.x * 0.5 + 0.5, index);
    triangle_stream.Append(vertex);

    triangle_stream.RestartStrip();
}