float4x4 WorldViewProjectionMatrix;

Texture BillboardTexture;
sampler BillboardTextureSampler = sampler_state 
{ 
	texture = <BillboardTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = clamp; 
	AddressV = clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(input.Position, WorldViewProjectionMatrix);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{	
    return tex2D(BillboardTextureSampler, input.TexCoord) * input.Color;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
