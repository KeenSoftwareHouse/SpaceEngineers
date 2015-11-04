#include "../MyEffectBase.fxh"

float2 HalfPixel;

float4x4 FaceMatrix;

int RandomTextureSize;
int IterationCount = 20;
float MainVectorWeight = 1.0f;

texture RandomTexture;
sampler RandomTextureSampler = sampler_state
{
	texture = <RandomTexture>;
	minfilter = POINT;
	magfilter = POINT;
	mipfilter = NONE;
	AddressU = WRAP; 
	AddressV = WRAP;
};

//	Texture contains scene from LOD0
TextureCube EnvironmentMap;
sampler EnvironmentMapTextureSampler = sampler_state 
{ 
	texture = <EnvironmentMap>;
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

float GetRandomNumber(int index)
{
	float2 coord = float2(index / (float)RandomTextureSize, 1);
	return tex2D(RandomTextureSampler, coord).x;
}

float3 GetRandomVector(int index)
{
	return float3(GetRandomNumber(index * 3), GetRandomNumber(index * 3 + 1), GetRandomNumber(index * 3 + 2));
}

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	//	We're using a full screen quad, no need for transforming vertices.
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoord;
	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float2 screenPos = input.TexCoord;
	screenPos.x = 1 - screenPos.x;

	float3 center = float3(0.5f, 0.5f, 0.5f);
	float3 normal = float3(screenPos, 1.0f) - center;
	normal = mul(normal, FaceMatrix);

	float4 environment = texCUBE(EnvironmentMapTextureSampler, normal);
	float4 ambientAcc = environment * MainVectorWeight;
	float ambientAccWeights = MainVectorWeight;

	for(int i = 0; i < IterationCount; i++)
	{
		float3 vec = GetRandomVector(i);
		if(dot(vec, normal) < 0)
		{
			vec = -vec;
		}

		// Weight and also n dot L (normal dot light vector)
		float weight = dot(normal, vec);

		ambientAcc += texCUBE(EnvironmentMapTextureSampler, vec) * weight;
		ambientAccWeights += weight;
	}

	float4 ambientColor = ambientAcc / ambientAccWeights;
	return float4(lerp(ambientColor.xyz, BacklightColorAndIntensity.xyz, BacklightColorAndIntensity.w * ambientColor.w), ambientColor.w );
}

technique BasicTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
