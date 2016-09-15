#ifndef DECALS_COMMON_H
#define DECALS_COMMON_H

#include <VertexTransformations.hlsli>

Texture2D<float> AlphamaskTexture : register(t3);
Texture2D<float4> ColorMetalTexture : register(t4);
Texture2D<float4> NormalGlossTexture : register(t5);
Texture2D<float4> ExtensionsTexture : register(t6);

float ReadAlphamaskTexture(float2 texcoord)
{
    return AlphamaskTexture.Sample(TextureSampler, texcoord);
}

void ReadColorMetalTexture(float2 texcoord, out float3 cm, out float metal)
{
    float4 cmSample = ColorMetalTexture.Sample(TextureSampler, texcoord);
    cm = cmSample.rgb;
    metal = cmSample.a;
}

void ReadNormalGlossTexture(float2 texcoord, out float3 normal, out float gloss)
{
    float4 ngSample = NormalGlossTexture.Sample(TextureSampler, texcoord);
    normal = normalize(ngSample.xyz * 2 - 1); // Source may be filtered
    gloss = ngSample.a;
}

void ReadExtensionsTexture(float2 texcoord, out float ao, out float emissive)
{
    float4 extSample = ExtensionsTexture.Sample(TextureSampler, texcoord);
    ao = extSample.r;
    emissive = extSample.g;
}

#endif // DECALS_COMMON_H