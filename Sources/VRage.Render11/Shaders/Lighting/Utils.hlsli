#ifndef LIGHTING_UTILS__
#define LIGHTING_UTILS__

static const float DIELECTRIC_F0 = 0.04f;

float3 SurfaceAlbedo(float3 baseColor, float metalness)
{
    return baseColor * (1 - metalness);
}

float3 SurfaceF0(float3 baseColor, float metalness)
{
    return lerp(DIELECTRIC_F0, baseColor, metalness);
}

float GlossToRoughness(float gloss)
{
    return 1.0f - gloss;
}

float RoughnessToGloss(float roughness)
{
    return 1.0f - roughness;
}

float remap_gloss(float g)
{
	float r = GlossToRoughness(g);
    return r * r;
}

float RoughnessToSpecular(float roughness)
{
    return mad(2.0f, pow(roughness, -2.0f), -2.0f);
}

float SpecularToGloss(float specular)
{
    return mad(-sqrt(2.0f), sqrt(rcp(specular + 2.0f)), 1.0f);
}

float GlossToSpecular(float gloss)
{
    return RoughnessToSpecular(GlossToRoughness(gloss));
}

#endif