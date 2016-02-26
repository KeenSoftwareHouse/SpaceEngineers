#ifndef BRDF_H__
#define BRDF_H__

#include <Lighting/DiffuseOrenNayar.h>
#include <Lighting/SpecularGGX.h>
#include <Lighting/Utils.h>

float toksvig_gloss(float gloss, float normalmap_len)
{
    float specular = GlossToSpecular(gloss);
    float ft = normalmap_len / lerp(specular, 1.0f, normalmap_len);
	ft = max(ft, 0.001f);
    return SpecularToGloss(ft * specular);
}

float3 importance_sample_ggx(float2 xi, float a, float3 N, out float pdf)
{
    float phi = 2 * M_PI * xi.x;
    float costheta = sqrt((1 - xi.y) / (mad(xi.y, a*a - 1, 1.0f)));
    float sintheta = sqrt(1 - costheta * costheta);
    pdf = DGGX(costheta, a);

    float3 H;
    H.x = sintheta * cos(phi);
    H.y = sintheta * sin(phi);
    H.z = costheta;
    float3 up = abs(N.z) < 0.9999f ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 tanx = normalize(cross(up, N));
    float3 tany = cross(N, tanx);

    return tanx * H.x + tany * H.y + N * H.z;
}

float3 MaterialRadiance(float3 albedo, float3 f0, float gloss, float3 N, float3 L, float3 V, float3 H)
{
	float nl = max(abs(dot(L, N)), 0.001);
	float nv = max(abs(dot(N, V)), 0.001);
    float nh = abs(dot(N, H));
    float vh = abs(dot(V, H));

    return DiffuseLight(nl, nv, albedo, gloss) + SpecularLight(f0, gloss, nl, nh, nv, vh);
}

#endif