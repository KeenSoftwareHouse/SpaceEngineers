#ifndef BRDF_H__
#define BRDF_H__

#include "DiffuseOrenNayar.hlsli"
#include "SpecularGGX.hlsli"
#include "SpecularPhong.hlsli"
#include "Utils.hlsli"

#define SpecularLight SpecularPhong

float ToksvigGloss(float gloss, float normalmap_len)
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

float3 MaterialRadiance(float3 albedo, float3 f0, float gloss, float3 N, float3 L, float3 V, float3 H, float dFactor)
{
	float ln = saturate(dot(L, N));
	float vn = saturate(dot(V, N));
    float nh = saturate(dot(N, H));
	float vh = saturate(dot(V, H));
	float lv = saturate(dot(L, V));

    float3 diffuseLight = DiffuseLight(ln, vn, lv, albedo, gloss) * dFactor;
    float3 specularLight = SpecularLight(ln, nh, vn, vh, f0, gloss);

    float3 light = diffuseLight;
    
    // HACK: saturated specular 
    // why: hotfix for too big values causing strange colors appearing in highlights (greenish / yellowish)
    // explanation: we are using R11G11B10_Float render target, where because of precision inconsistency between channels, 
    // high values will outweight some channels (red and green) over the other (blue)
    // proper fix: different render target format (research will be needed) or better postprocessing, taking into account disballance in precision
    light += saturate(specularLight);

    return light * ln;
}

#endif
