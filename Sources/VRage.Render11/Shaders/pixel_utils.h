#ifndef PIXEL_UTILS_H
#define PIXEL_UTILS_H

#include <frame.h>

float3 compute_screen_ray(float2 uv)
{
    const float ray_x = 1. / frame_.projection_matrix._11;
    const float ray_y = 1. / frame_.projection_matrix._22;
    return float3(lerp(-ray_x, ray_x, uv.x), -lerp(-ray_y, ray_y, uv.y), -1.);
}

float compute_depth(float hw_depth)
{
    return -linearize_depth(hw_depth, frame_.projection_matrix);
}

// http://www.thetenthplanet.de/archives/1180
float3x3 PixelTangentSpace(float3 N, float3 dp1, float3 dp2, float2 uv)
{
    float2 duv1 = ddx(uv);
    float2 duv2 = ddy(uv);
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);

    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

float3x3 PixelTangentSpace(float3 N, float3 pos, float2 uv)
{
    return PixelTangentSpace(N, ddx(pos), ddy(pos), uv);
}

// NOTE: Below are the established conventions to interpret all
// the new assets consistentently. We assume now that the input
// normalmaps are correct

void adjust_normalmap_precomputed_tangents(inout float3 normalmap)
{
    normalmap.y *= -1;
}

void adjust_normalmap_no_precomputed_tangents(inout float3 normalmap)
{
    // CHECK-ME: Check if it is possible to make it consistent with above
    // convention by modifying PixelTangentSpace()
    normalmap.x *= -1;
}

float3 NormalBuildTangent(float3 normalmap, float3 normal, float2 texcoord0, float3 pos_ws, out float normalLength)
{
    normalmap = normalmap * 2 - 1;
    adjust_normalmap_no_precomputed_tangents(normalmap);
    normalLength = length(normalmap);

    float3x3 tangent_to_world;
    float3 dPosX = ddx(pos_ws);
    float3 dPosY = ddy(pos_ws);

    tangent_to_world = PixelTangentSpace(normal, dPosX, dPosY, texcoord0);

    return normalize(mul(normalmap, tangent_to_world));
}

float3 Normal(float3 normalmap, float4 tangent, float3 normal, out float normalLength)
{
    normalmap = normalmap * 2 - 1;
    adjust_normalmap_precomputed_tangents(normalmap);
    normalLength = length(normalmap);

    float3 T = tangent.xyz;
    float3 B = cross(T, normal) * tangent.w;
    float3x3 tangent_to_world = float3x3(T, B, normal);

    return normalize(mul(normalmap, tangent_to_world));
}

#endif // PIXEL_UTILS_H
