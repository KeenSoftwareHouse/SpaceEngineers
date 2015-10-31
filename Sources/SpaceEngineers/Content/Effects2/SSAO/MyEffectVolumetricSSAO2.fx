#include "../MyEffectBase.fxh"


Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT>; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = none; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture NormalsTexture;
sampler NormalsTextureSampler = sampler_state 
{ 
	texture = <NormalsTexture>; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = none; 
	AddressU = WRAP; 
	AddressV = WRAP;
};


float2 HalfPixel;
float3 FrustumCorners[4];

float4x4 ViewMatrix;
float4x4 ProjectionMatrix;



struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 TexCoordAndCornerIndex	: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;	
	float3 FrustumCorner : TEXCOORD1; 
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoordAndCornerIndex.xy + HalfPixel;
	output.FrustumCorner = FrustumCorners[input.TexCoordAndCornerIndex.z];
	return output;
}



//
// Volumetric Ambient Occlusion
//	Laszlo Szirmay-Kalos at el.
// 
// See:
//
// http://www.iit.bme.hu/~szirmay/ambient8.pdf
//

#define NUM_SAMPLES 8
#define NUM_FOURS_BATCHES 2

// SSAOParams.x = minRadius
// SSAOParams.y = maxRadius
// SSAOParams.z = radiusGrowZscale
// SSAOParams.w = camera zfar
uniform float4	SSAOParams;				

// SSAOParams2.x = bias
// SSAOParams2.y = fallof
// SSAOParams2.z = occlusion samples normalization value * color scale
uniform float4	SSAOParams2;
	
uniform float4	ProjViewPortCoef;

uniform float4	OcclPos[NUM_SAMPLES];
uniform float4	OcclPosFlipped[NUM_SAMPLES];	

float Contrast = 1.0f;

		
void CalcOrthonormalBasisNoY(float3 dir,out float3 right,out float3 up)
{
    right   = float3(dir[2],0,-dir[0]);
    up      = cross(dir,right);
}

float		CalcAmbientOCBatch4(in int i, in float2 rndRot, in float3 wrldPos, in float3 T, in float3 B, in float zfar, in float radius, in float fallof)
{	
		float2	roffs[4];			
		roffs[0]		= rndRot.yx * OcclPos[i].xy   + rndRot.xy * OcclPosFlipped[i].xy;
		roffs[1]		= rndRot.yx * OcclPos[i+1].xy + rndRot.xy * OcclPosFlipped[i+1].xy;
		roffs[2]		= rndRot.yx * OcclPos[i+2].xy + rndRot.xy * OcclPosFlipped[i+2].xy;
		roffs[3]		= rndRot.yx * OcclPos[i+3].xy + rndRot.xy * OcclPosFlipped[i+3].xy;
	
// construct world pos sample on plane
		float3	occlPos[4];		
		occlPos[0]		= wrldPos + roffs[0].x * T + roffs[0].y * B;
		occlPos[1]		= wrldPos + roffs[1].x * T + roffs[1].y * B;
		occlPos[2]		= wrldPos + roffs[2].x * T + roffs[2].y * B;
		occlPos[3]		= wrldPos + roffs[3].x * T + roffs[3].y * B;

// view space  version

		float2	occlProjPos[4];

		
		occlProjPos[0]	= occlPos[0].xy / occlPos[0].z;	
		occlProjPos[1]	= occlPos[1].xy / occlPos[1].z;	
		occlProjPos[2]	= occlPos[2].xy / occlPos[2].z;	
		occlProjPos[3]	= occlPos[3].xy / occlPos[3].z;	

		 
		occlProjPos[0]	= GetScreenSpaceTextureCoord(mul(float4(occlPos[0], 1), ProjectionMatrix), HalfPixel);
		occlProjPos[1]	= GetScreenSpaceTextureCoord(mul(float4(occlPos[1], 1), ProjectionMatrix), HalfPixel);
		occlProjPos[2]	= GetScreenSpaceTextureCoord(mul(float4(occlPos[2], 1), ProjectionMatrix), HalfPixel);
		occlProjPos[3]	= GetScreenSpaceTextureCoord(mul(float4(occlPos[3], 1), ProjectionMatrix), HalfPixel);
		   
// sample depth
		float4	zi;
		zi.x			= DecodeFloatRGBA(tex2D(DepthsRTSampler,occlProjPos[0]));
		zi.y			= DecodeFloatRGBA(tex2D(DepthsRTSampler,occlProjPos[1]));
		zi.z			= DecodeFloatRGBA(tex2D(DepthsRTSampler,occlProjPos[2]));
		zi.w			= DecodeFloatRGBA(tex2D(DepthsRTSampler,occlProjPos[3]));
		zi				*= zfar;
	
// compute ao portion of the sample	
		float4	zExtreme	= float4(OcclPos[i].w,OcclPos[i+1].w,OcclPos[i+2].w,OcclPos[i+3].w) * radius;	
//viewspace version
		float4	zmin		= -float4(occlPos[0].z, occlPos[1].z, occlPos[2].z, occlPos[3].z) - zExtreme;
	
		float4	D			= float4(zExtreme) * 2;	
		float4	dz			= min(max(zi - zmin,0),D);
	
// distant occluder attenuation
		float4 x			= saturate((zmin - zi) * fallof);
		float4 attDz		= x * x * D;

		float4 k			= step(zmin, zi);
		dz					= dz * k + (1-k) * attDz;
	
		return dot(float(1).xxxx, dz);
		//return x;
		//return zi * zfar;
}

float4 SSAO(float2 uv, float3 eyeDir, float2 screenPos)
{	
	//float4 cl = tex2D(DepthsRTSampler, uv);
	//return float4(cl.x,cl.y,cl.z,length(cl));
	//return float4(cl.x,cl.y,cl.z,0.5f);

	float	depth	=  DecodeFloatRGBA(tex2D(DepthsRTSampler, uv));

	if (IsDepthBackground(depth))
	{
		//discard;
		return float4(0,0,0,0.0f);
	}

	float	zfar	= SSAOParams.w;
		
	float	radius	= SSAOParams.x + (1 - pow(SSAOParams.z, - depth)) * (SSAOParams.y - SSAOParams.x);
	float	bias	= radius * SSAOParams2.x;
	float	fallof	= SSAOParams2.y;
	
//viewspace pos
	float3 viewPos = GetViewPositionFromDepth(depth, eyeDir);
		 
	
	float3 N;
	float3 T = float3(1,0,0);
	float3 B = float3(0,1,0);
	
// view space normal	
	N = GetNormalVectorFromRenderTargetNormalized(tex2D(NormalsTextureSampler, uv));
	N = normalize(mul(N, (float3x3)ViewMatrix));
				

//	CalcOrthonormalBasisNoY(N,T,B);
				
	float2	rndRot;		
	float	noise	= dot(screenPos, float2(8.1,5.7));		
	sincos(noise, rndRot.x, rndRot.y);
	rndRot	*= radius;	

// view space
	viewPos += 0.5 * (bias + radius) * N;
	
	float	occl	= 0;
	for (int i = 0; i < NUM_FOURS_BATCHES; i++)
	{	
		occl += CalcAmbientOCBatch4(i * 4, rndRot, viewPos, T, B, zfar, radius, fallof);
	}
	
// normalize ao by the sphere volume
	occl =  saturate(occl * SSAOParams2.z / radius );	 

	//occl = max((occl - 0.5f)*Contrast + 0.5f,0.2f);
	occl = (occl - 0.5f)*Contrast + 0.5f;

	//return float(occl).xxxx;							 
	return float4(float(0).xxx, 1 - occl);							 
	//return float4(float(0).xxx, 1.0f);							 

	
	//float occl = CalcAmbientOCBatch4(0, rndRot, viewPos, T, B, zfar, radius, fallof);

	//return float4(rndRot.x, rndRot.y, 0,1);
	//return float4(occl.xxx, 1) * SSAOParams2.z;
}	







float4 PixelShaderFunction(VertexShaderOutput input, float2 screenPosition : VPOS) : COLOR0
{
	return SSAO(input.TexCoord, input.FrustumCorner.xyz, screenPosition);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}
