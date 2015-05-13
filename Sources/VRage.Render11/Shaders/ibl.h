#ifndef IBL_H__
#define IBL_H__

#include <brdf.h>
#include <frame.h>

TextureCube<float4> SkyboxTex : register( MERGE(t,SKYBOX_SLOT) );
TextureCube<float4> SkyboxIBLTex : register( MERGE(t,SKYBOX_IBL_SLOT) );
Texture2D<float2> AmbientBRDFTex : register( MERGE(t,AMBIENT_BRDF_LUT_SLOT) );

TextureCube<float4> Skybox2Tex : register( MERGE(t,SKYBOX2_SLOT) );
TextureCube<float4> Skybox2IBLTex : register( MERGE(t,SKYBOX2_IBL_SLOT) );

static const float IBL_MAX_MIPMAP = 8;

float3 SkyboxColor(float3 v)
{
	float3 sample = SkyboxTex.Sample(TextureSampler, v).xyz;
	float3 sample1 = Skybox2Tex.Sample(TextureSampler, v).xyz;
	return lerp(sample, sample1, smoothstep(0, 1, frame_.skyboxBlend));
}

float3 ambient_specular(float3 f0, float gloss, float3 N, float3 V)
{
	float nv = saturate(dot(N, V));

	float3 R = 2 * nv * N - V;

	float3 sample = SkyboxIBLTex.SampleLevel(TextureSampler, R, (1 - gloss) * IBL_MAX_MIPMAP).xyz;
	float3 sample1 = Skybox2IBLTex.SampleLevel(TextureSampler, R, (1 - gloss) * IBL_MAX_MIPMAP).xyz;
	float2 env_brdf = AmbientBRDFTex.Sample(DefaultSampler, float2(gloss, nv));

	return lerp(sample, sample1, smoothstep(0, 1, frame_.skyboxBlend)) * ( f0 * env_brdf.x + env_brdf.y) * frame_.env_mult;
}

float3 ambient_diffuse(float3 N)
{
	float3 sample = SkyboxIBLTex.SampleLevel(TextureSampler, N, IBL_MAX_MIPMAP).xyz;
	float3 sample1 = Skybox2IBLTex.SampleLevel(TextureSampler, N, IBL_MAX_MIPMAP).xyz;

	return lerp(sample, sample1, smoothstep(0, 1, frame_.skyboxBlend)) * frame_.env_mult;
}


#endif