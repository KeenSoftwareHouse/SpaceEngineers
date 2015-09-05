#include "../MyEffectBase.fxh"

float4x4	ViewProjectionMatrix;

Texture Texture;
sampler TextureSampler = sampler_state 
{ 
	texture = <Texture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = clamp; 
	AddressV = clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	//float4 worldPosition = mul(input.Position, WorldMatrix);
    //output.Position = mul(worldPosition, ViewProjectionMatrix);
    
    output.Position = mul(input.Position, ViewProjectionMatrix);
    
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    //return float4(1, 0, 0, 1);
    
    return tex2D(TextureSampler, input.TexCoord) * input.Color;    
    //return tex2D(TextureSampler, input.TexCoord);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
