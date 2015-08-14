//	Pixel shader applies a one dimensional gaussian blur filter.
//	This is used twice, first to blur horizontally, and then again to blur vertically.

#include "../MyEffectBase.fxh"

Texture SourceTexture;
sampler SourceTextureSampler = sampler_state 
{ 
	texture = <SourceTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

float2		HalfPixel;

#define SAMPLE_COUNT 15
float2 SampleOffsets[SAMPLE_COUNT];
float SampleWeights[SAMPLE_COUNT];

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
    output.TexCoord = input.TexCoord + HalfPixel;
    return output;
}

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 c = 0;
    
    // Combine a number of weighted image filter taps.
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        c += tex2D(SourceTextureSampler, texCoord + SampleOffsets[i]) * SampleWeights[i];
    }
    
    return c;
}

technique Technique1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}