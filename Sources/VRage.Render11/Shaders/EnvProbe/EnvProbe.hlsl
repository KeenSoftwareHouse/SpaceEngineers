// @skipCache
#include <Lighting/Brdf.hlsli>

Texture2DArray<float4> EnvProbeArray : register(t0);
TextureCube<float4> EnvProbeCube : register(t0);
RWTexture2DArray<float4> FaceOutput : register(u0);
RWTexture2D<float2> Output2C : register(u0);

SamplerState TextureSampler : register(s0);


cbuffer Constants : register(b0)
{
	uint resolution_;
	uint faceID_;
	float gloss_;
	uint mipoffset_;
};

[numthreads(8, 8, 1)]
void copy_cube(uint3 dispatchThreadID : SV_DispatchThreadID)
{
	const uint2 texel = dispatchThreadID.xy;
	float invres = 1 / (float)resolution_;
	float2 frontPlane = 2 * (texel + 0.5) / (float)resolution_ - 1;
		float3 N = normalize(float3(frontPlane, 1));
		N.y *= -1;
	N = mul(N, cubemap_face_onb(faceID_));

	FaceOutput[uint3(texel, faceID_)] = EnvProbeCube.SampleLevel(TextureSampler, N, mipoffset_);
}

[numthreads(8, 8, 1)]
void convolve_reference(uint3 dispatchThreadID : SV_DispatchThreadID)
{
	const uint2 texel = dispatchThreadID.xy;
	float2 frontPlane = 2 * (texel + 0.5) / (float)resolution_ - 1;
	float3 N = normalize(float3(frontPlane, 1));
	N.y *= -1;
	N = mul(N, cubemap_face_onb(faceID_));

	float4 sum = 0;

	float a = remap_gloss(gloss_);

	for (int f = 0; f < 6; f++)
	{
		float3x3 face_onb = cubemap_face_onb(f);
		for (uint i = 0; i < 256 * 256; i++)
		{
			uint x = i / 256;
			uint y = i % 256;
			float2 xy = float2(x, y);

			float2 uv = (xy + 0.5) / (float)256;
			float3 R = normalize(mul(float3(uv * 2 - 1, 1), face_onb));
			R.y = -R.y;
			float nl = saturate(dot(N, R));

			float dS = texel_coord_solid_angle(x, y, 256);
			float nh = nl;
			[branch]
			if (nl>0)
			{
				sum += float4(EnvProbeCube.SampleLevel(TextureSampler, R, mipoffset_).xyz * d_ggx(nh, a) * nl * dS, 
					dS);

				//sum += 0.25 * d_ggx(nh, a) * nl * dS;
			}
		}
	}
	
	FaceOutput[uint3(texel, faceID_)] = sum;
}


[numthreads(8, 8, 1)]
void preintegrate_brdf(uint3 dispatchThreadID : SV_DispatchThreadID)
{
	float gloss = dispatchThreadID.x / 256.f;
	float nv = (dispatchThreadID.y + 1) / 256.f;	

	float3 V = float3(sqrt(1-nv*nv), 0, nv);

	float2 acc = 0;
	int SAMPLES_NUM = 1024;
	for(int i=0; i<SAMPLES_NUM; i++)
	{
		float a = remap_gloss(gloss);
		float pdf;

		float2 xi = hammersley(i, SAMPLES_NUM);
		float3 H = importance_sample_ggx(xi, a, float3(0,0,1), pdf);

		float vh = dot(V, H);
		float3 L = 2 * vh * H - V;

		vh = saturate(vh);
		float nl = saturate(L.z);
		float nh = saturate(H.z);

		if(nl > 0)
		{
			float G = g_smithschlick(nl, nv, a) * vh / (nh * nv);

			float f5 = pow(1 - vh, 5);
			acc.x += (1-f5) * G;
			acc.y += f5 * G;
		}
	}

	Output2C[dispatchThreadID.xy] = acc / (float)SAMPLES_NUM;
}