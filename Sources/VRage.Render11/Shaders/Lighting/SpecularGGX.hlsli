#ifndef SPECULAR_GGX__
#define SPECULAR_GGX__

#include <Math/Math.hlsli>
#include "Utils.hlsli"

// microfacet distribution
float DGGX(float nh, float a)
{
    float a2 = a*a;
    float normalizationConstant = a2 * M_1_PI;
	float nh2 = nh * nh;
	float b = mad(nh2, (a2 - 1), 1);
    return normalizationConstant / (b * b);
}

float G1SmithSchlickGGX(float nx, float k)
{
    return mad(1 - k, nx, k);
}

// geometric / visibility
float GSmithSchlickGGX(float ln, float vn, float a)
{
	float k = 0.5f * a;
	return 0.25f / (G1SmithSchlickGGX(ln, k) * G1SmithSchlickGGX(vn, k));
}

// fresnel
float3 FSchlick(float vh, float3 f0)
{
	float fr = exp2((-5.55473 * vh - 6.98316) * vh);
	return mad(1 - f0, fr, f0);
}

float3 SpecularGGX(float ln, float nh, float vn, float vh, float3 f0, float gloss)
{
    float roughness = GlossToRoughness(gloss);
    float a = max(roughness*roughness, 2e-3);
	return DGGX(nh, a) * GSmithSchlickGGX(ln, vn, a) * FSchlick(vh, f0);
}

#endif
