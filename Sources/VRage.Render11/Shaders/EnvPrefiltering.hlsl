#ifndef ENV_PREFILTERING__
#define ENV_PREFILTERING__

#include <common.h>
#include <brdf.h>

TextureCube<float4> ProbeTex : register( t0 );
Texture2DArray<float4> PrevMipmap : register( t0 );
RWTexture2DArray<float4> UAOutput : register( u0 );

cbuffer Constants : register( b1 )
{
	uint SamplesNum; // power of 2
	uint EnvProbeRes;
	uint MipResolution;
	uint FaceId;
	float Gloss_Blend;
}

[numthreads(8, 8, 1)]
void buildMipmap(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	UAOutput[uint3(texel, 0)] = (
	 	PrevMipmap[uint3(texel * 2, 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(1, 0), 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(0, 1), 0)] + 
	 	PrevMipmap[uint3(texel * 2 + uint2(1, 1), 0)]) * 0.25;
}

[numthreads(8, 8, 1)]
void prefilter(uint3 dispatchThreadID : SV_DispatchThreadID) {

	uint2 texel = dispatchThreadID.xy;
	float a = remap_gloss(Gloss_Blend);

	//float2 frontPlane = 2 * (texel + 0.5) / (float) MipResolution - 1;
	// for smooth filtering strech for whole plane (no snapping to texel location)
	float2 frontPlane = 2 * (texel) / (float) (MipResolution - 1) - 1;
	float3 N = normalize(float3(frontPlane, 1));

	if(MipResolution == 1) {
		N = float3(0, 0, 1);
	}

	N.y *= -1;
	N = mul(N, cubemap_face_onb(FaceId));

	float3 V = N;

	float4 acc = 0;

	for(uint i=0; i< SamplesNum; ++i) {
		float2 xi = hammersley2d(i, SamplesNum);
		float pdf;
		float3 H = importance_sample_ggx(xi, a, N, pdf);

		float3 L = 2 * dot( V, H ) * H - V;
	    float NL = saturate(dot( N, L ));
	    float VL = saturate(dot( V, L ));
	    float NH = saturate(dot( N, H ));
	    float VH = saturate(dot( V, H ));

	    if( NL > 0 )
	    {
	    	float texelSolidAngle = 4 * M_PI / (6 * EnvProbeRes * EnvProbeRes);
	    	float sampleSolidAngle = 1 / ( pdf * SamplesNum );
	    	float lod = Gloss_Blend == 1 ? 0 : 0.5 * log2((float)(sampleSolidAngle/texelSolidAngle));
	    	float3 sample = (float3)ProbeTex.SampleLevel(LinearSampler, L, lod);
	    	acc.xyz += sample * NL;
	    	acc.w += NL;
	    }
	}

	acc.xyz /= acc.w;
	UAOutput[uint3(texel, 0)] = acc;
}

Texture2DArray<float3> Face0 : register( t0 );
Texture2DArray<float3> Face1 : register( t1 );
RWTexture2DArray<float3> BlendOutput : register( u0 );

[numthreads(8, 8, 1)]
void blend(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	if(any(texel >= MipResolution)) return;

	BlendOutput[uint3(texel, 0)] = lerp(Face0[uint3(texel, 0)].xyz, Face1[uint3(texel, 0)].xyz, Gloss_Blend);
	//BlendOutput[uint3(texel, 0)] = abs(Face0[uint3(texel, 0)].xyz - Face1[uint3(texel, 0)].xyz);
}

#endif