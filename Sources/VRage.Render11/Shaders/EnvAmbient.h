#ifndef ENVAMBIENT_H__
#define ENVAMBIENT_H__

#include <brdf.h>
#include <frame.h>

Texture2D<float2> AmbientBRDFTex : register( MERGE(t,AMBIENT_BRDF_LUT_SLOT) );

TextureCube<float4> SkyboxTex : register( MERGE(t,SKYBOX_SLOT) );
TextureCube<float4> SkyboxIBLTex : register( MERGE(t,SKYBOX_IBL_SLOT) );

TextureCube<float4> Skybox2Tex : register( MERGE(t,SKYBOX2_SLOT) );
TextureCube<float4> Skybox2IBLTex : register( MERGE(t,SKYBOX2_IBL_SLOT) );

//#define ENV_IBL_SLOT	20
//TextureCube<float4> ProbeTex : register( MERGE(t,ENV_IBL_SLOT) );

static const float IBL_MAX_MIPMAP = 8;

#define SKYBOX_BLENDING

float3 SkyboxColor(float3 v)
{
	float3 sample = SkyboxTex.Sample(TextureSampler, v).xyz;		
	float3 sample1 = Skybox2Tex.Sample(TextureSampler, v).xyz;
	
	return lerp(sample, sample1, frame_.skyboxBlend);
}

float3 SkyboxColorLod(float3 v, float lod)
{
	float3 sample = SkyboxTex.Sample(TextureSampler, v).xyz;	
	float3 sample1 = Skybox2Tex.SampleLevel(TextureSampler, v, lod).xyz;

	return lerp(sample, sample1, frame_.skyboxBlend);
}

float3 ambient_specular(float3 f0, float gloss, float3 N, float3 V)
{
	float nv = saturate(dot(N, V));

	float3 R = 2 * nv * N - V;
	//R.x = -R.x;

	float3 sample = SkyboxIBLTex.SampleLevel(TextureSampler, R, (1 - gloss) * IBL_MAX_MIPMAP).xyz;
	float3 sample1 = Skybox2IBLTex.SampleLevel(TextureSampler, R, (1 - gloss) * IBL_MAX_MIPMAP).xyz;
	float2 env_brdf = AmbientBRDFTex.Sample(DefaultSampler, float2(gloss, nv));

	return lerp(sample, sample1, smoothstep(0, 1, frame_.skyboxBlend)) * ( f0 * env_brdf.x + env_brdf.y) * frame_.env_mult;
}

float3 ambient_diffuse(float3 f0, float gloss, float3 N, float3 V)
{
	//N.x = -N.x;
	float3 sample = SkyboxIBLTex.SampleLevel(TextureSampler, N, IBL_MAX_MIPMAP).xyz;
	float3 sample1 = Skybox2IBLTex.SampleLevel(TextureSampler, N, IBL_MAX_MIPMAP).xyz;

	return lerp(sample, sample1, smoothstep(0, 1, frame_.skyboxBlend)) * frame_.env_mult;
}

float3 ambient_diffuse(float3 N)
{
	return ambient_diffuse(0, 0, N, 0);
}

static const uint SamplesNum = 64;
static const float EnvProbeRes = 2048;
static const float EnvProbeMips = 11;

float3 ambientSpecularIS(float3 f0, float gloss, float3 N, float3 V) {
	float4 acc = 0;

	const float SamplesRcp = 1 / SamplesNum;
	const float NV = saturate(dot(N, V));

	[unroll]
	for(uint i=0; i< SamplesNum; ++i) {
		float2 xi = hammersley2d(i, SamplesNum);
		float pdf;
		float a = remap_gloss(gloss);
		float3 H = importance_sample_ggx(xi, a, N, pdf);

		float3 L = 2 * dot( V, H ) * H - V;
        float NL = saturate(dot( N, L ));
        float VL = saturate(dot( V, L ));
        float NH = saturate(dot( N, H ));
        float VH = saturate(dot( V, H ));
        [flatten]
        if( NL > 0 )
        {
        	float texelSolidAngle = 4 * M_PI / (6 * EnvProbeRes * EnvProbeRes);
        	float sampleSolidAngle = 1 / ( pdf * SamplesNum );
        	float lod = gloss == 1 ? 0 : 0.5 * log2((float)(sampleSolidAngle/texelSolidAngle));
        	float3 sample = SkyboxColorLod(L, lod);
        	acc.xyz += sample * NL;
        	acc.w += NL;
        }
	}

	float3 env = acc.xyz / acc.w;
	float2 env_brdf = AmbientBRDFTex.Sample(DefaultSampler, float2(gloss, NV));
	return env * ( f0 * env_brdf.x + env_brdf.y);
}


#endif