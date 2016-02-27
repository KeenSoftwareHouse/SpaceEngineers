#ifndef SPECULAR_GGX__
#define SPECULAR_GGX__

#include <Lighting/Utils.h>
#include <Math/math.h>

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
float GSmithSchlickGGX(float nl, float nv, float a)
{
	float k = 0.5f * a;
	return 0.25f / (G1SmithSchlickGGX(nl, k) * G1SmithSchlickGGX(nv, k));
}

// fresnel
float3 FSchlick(float3 f0, float vh)
{
	float fr = exp2((-5.55473 * vh - 6.98316) * vh);
	return mad(1 - f0, fr, f0);
}

float3 SpecularBRDFGGX(float3 f0, float gloss, float nl, float nh, float nv, float vh)
{
    float roughness = GlossToRoughness(gloss);
    float a = max(roughness*roughness, 2e-3);
	return saturate(DGGX(nh, a) * GSmithSchlickGGX(nl, nv, a) * FSchlick(f0, vh)) / 4.0f;
}

#define SpecularLight SpecularBRDFGGX

#endif