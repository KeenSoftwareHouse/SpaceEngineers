#ifndef PIXEL_UTILS_H
#define PIXEL_UTILS_H

#define AO_BUMP_POW 4
#define NORMAL_DOT_FACTOR 4

float3x3 PixelTangentSpace(float3 N, float3 pos_ddx, float3 pos_ddy, float2 uv_ddx, float2 uv_ddy)
{
    float3 dpxperp = cross(N, pos_ddx);
    float3 dpyperp = cross(pos_ddy, N);

    float3 T = dpyperp * uv_ddx.x + dpxperp * uv_ddy.x;
    float3 B = dpyperp * uv_ddx.y + dpxperp * uv_ddy.y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

// http://www.thetenthplanet.de/archives/1180
float3x3 PixelTangentSpace(float3 N, float3 pos_ddx, float3 pos_ddy, float2 uv)
{
    float2 uv_ddx = ddx(uv);
    float2 uv_ddy = ddy(uv);
    return PixelTangentSpace(N, pos_ddx, pos_ddy, uv_ddx, uv_ddy);
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

    float3 dPosX = ddx(pos_ws);
    float3 dPosY = ddy(pos_ws);

    float3x3 tangent_to_world = PixelTangentSpace(normal, dPosX, dPosY, texcoord0);

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

// Calculate best effort alpha for normal map even when where given alpha is not defined
float ComputeNormalMapAlphaBestEffort(float3 normal, float alpha)
{
    // Find distance from basic normal and multiply by a grow factor
    float alphaN = saturate((1 - dot(normal, float3(0, 0, 1))) * NORMAL_DOT_FACTOR);
    return max(alpha, alphaN);
}

// Calculate best effor ambient occlusion from current normal and perturbed normal
float ComputeAmbientOcclusionBestEffort(float3 currentNormal, float3 perturbedNormal)
{
    return pow(dot(currentNormal, perturbedNormal), AO_BUMP_POW);
}

#endif // PIXEL_UTILS_H
