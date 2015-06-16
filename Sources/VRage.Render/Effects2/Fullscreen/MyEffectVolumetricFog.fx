#include "../MyEffectBase.fxh"
#include "../MyPerlinNoise.fxh"


Texture SourceRT;
sampler SourceRTSampler = sampler_state 
{ 
	texture = <SourceRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture NormalsTexture;
sampler NormalsTextureSampler = sampler_state 
{ 
	texture = <NormalsTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};


float2 HalfPixel;
float3 FrustumCorners[4];

float4x4 WorldMatrix;
float4x4 ViewProjectionMatrix;
float4x4 CameraMatrix;
float3 CameraPos;

float2 Scale = float2(1.0f, 1.0f);


struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 TexCoordAndCornerIndex	: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;	
	//float3 WorldPos : TEXCOORD1;
	float3 FrustumCorner : TEXCOORD1; 
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = (input.TexCoordAndCornerIndex.xy + HalfPixel) * Scale;
	output.FrustumCorner = FrustumCorners[input.TexCoordAndCornerIndex.z];
	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input, float2 screenPosition : VPOS) : COLOR0
{
	float4 encodedDepth = tex2D(DepthsRTSampler, input.TexCoord);
	float depth = DecodeFloatRGBA(encodedDepth)* FAR_PLANE_DISTANCE;	
	return CalculateFogLinear(depth);
}



float4 SkipBackgroundPixelShaderFunction(VertexShaderOutput input, float2 screenPosition : VPOS) : COLOR0
{
	float4 encodedDepth = tex2D(DepthsRTSampler, input.TexCoord);
	float depth = DecodeFloatRGBA(encodedDepth);
	float depthForTest = GetViewDistanceFromDepth(depth, input.FrustumCorner);
	float4 sourceColor = tex2D(SourceRTSampler, input.TexCoord);

	float3 normal = GetNormalVectorFromRenderTarget(tex2D(NormalsTextureSampler, input.TexCoord).xyz);
	float blend = length(normal);

	//Linear fog

	float4 returnColor = float4(0,0,0,0);

	if (depthForTest < 50000)	
		returnColor = CalculateFogLinear(depthForTest);
	
	return returnColor;
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}

technique SkipBackgroundTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 SkipBackgroundPixelShaderFunction();
	}
}
