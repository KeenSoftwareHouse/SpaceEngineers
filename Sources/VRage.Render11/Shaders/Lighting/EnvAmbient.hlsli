#ifndef ENVAMBIENT_H__
#define ENVAMBIENT_H__

#include "Brdf.hlsli"
#include <Frame.hlsli>

Texture2D<float2> AmbientBRDFTex : register( MERGE(t,AMBIENT_BRDF_LUT_SLOT) );

TextureCube<float4> SkyboxTex : register( MERGE(t,SKYBOX_SLOT) );
TextureCube<float4> SkyboxIBLTex : register( MERGE(t,SKYBOX_IBL_SLOT) );

//#define ENV_IBL_SLOT	20
//TextureCube<float4> ProbeTex : register( MERGE(t,ENV_IBL_SLOT) );

static const float IBL_MAX_MIPMAP = 8;

float3 ApplySkyboxOrientation(float3 v)
{
    v = mul(float4(-v, 0.0f), frame_.Environment.background_orientation).xyz;
    // This is because DX9 code does the same (see MyBackgroundCube.cs)
    v.z *= -1;
    return v;
}
float3 SkyboxColor(float3 v)
{
    v = ApplySkyboxOrientation(v);
	float3 sample = SkyboxTex.Sample(TextureSampler, v).xyz;		
	
    return sample;
}

float3 SkyboxColorReflected(float3 v)
{
    v = -ApplySkyboxOrientation(v);
    float3 sample = SkyboxTex.Sample(TextureSampler, v).xyz;

    return sample;
}

float3 ambient_specular(float3 f0, float gloss, float3 N, float3 V)
{
	float nv = saturate(dot(N, V));
	float3 R = -reflect(V, N);
	R.x = -R.x;

	float3 sample = SkyboxIBLTex.SampleLevel(TextureSampler, R, (1 - gloss) * IBL_MAX_MIPMAP).xyz;
	float2 env_brdf = AmbientBRDFTex.Sample(DefaultSampler, float2(gloss, nv));

    return sample * (f0 * env_brdf.x + env_brdf.y) * frame_.Light.ambientSpecularFactor;
}

float3 ambient_diffuse(float3 N)
{
    N.x = -N.x;

	float3 skybox = SkyboxIBLTex.SampleLevel(TextureSampler, N, IBL_MAX_MIPMAP).xyz;
    return skybox * frame_.Light.ambientDiffuseFactor;
}

float3 ambient_diffuse(float3 albedo, float3 normal)
{
	return albedo * ambient_diffuse(normal);
}

float3 ambient_global(float3 color, float dist)
{
    dist = clamp(dist, 0, 1000);
    float ambient = frame_.Light.ambientGlobalMultiplier * exp(-dist * (1 - frame_.Light.ambientGlobalDensity)) + frame_.Light.ambientGlobalMinimum;

    return color * ambient;
}

#endif
