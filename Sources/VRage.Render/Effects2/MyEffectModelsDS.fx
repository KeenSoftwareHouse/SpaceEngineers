#include "MyEffectBase.fxh"

//	This shader renders a model with diffuse and specular textures, so it requires certain vertex shader data

float4x4	WorldMatrix;
float4x4	ViewMatrix;
float4x4	ProjectionMatrix;
float3	    DiffuseColorAdd;

Texture TextureDiffuseAndSpecular;
sampler TextureDiffuseAndSpecularSampler = sampler_state 
{ 
	texture = <TextureDiffuseAndSpecular> ; 
	mipfilter = LINEAR; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

struct VertexShaderInput_DS
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput_DS
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float ViewDistance : TEXCOORD1;
    float3 Normal : TEXCOORD2;
};

VertexShaderOutput_DS VertexShaderFunction_DS(VertexShaderInput_DS input)
{
    VertexShaderOutput_DS output;

	output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
    output.ViewDistance = length(output.Position.xyz);
    output.Position = mul(output.Position, ProjectionMatrix);    
    output.TexCoord = input.TexCoord;
    output.Normal = mul(input.Normal, (float3x3)WorldMatrix);
    return output;
}

GbufferPixelShaderOutput PixelShaderFunction_DS(VertexShaderOutput_DS input)
{
	float3 normal = normalize(input.Normal);

	float4 diffuseAndSpecularTexture = tex2D(TextureDiffuseAndSpecularSampler, input.TexCoord);
    float3 diffuseTexture = diffuseAndSpecularTexture.rgb;
    float specularTexture = diffuseAndSpecularTexture.a;		//	Specular is stored in alpha channel of diffuse texture
    
	//	Output into MRT
	GbufferPixelShaderOutput output = GetGbufferPixelShaderOutput(normal, diffuseTexture, input.ViewDistance);
	output.Diffuse.rgb = output.Diffuse.rgb + DiffuseColorAdd;
	return output;
}

technique Technique_RenderQualityNormal
{
    pass Pass1
    {
        Texture[0] = <TextureDiffuseAndSpecular>;			//	I think this Texture[] assignement isn't needed but I rather did it
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DS();
    }
}

technique Technique_RenderQualityHigh
{
    pass Pass1
    {
        Texture[0] = <TextureDiffuseAndSpecular>;			//	I think this Texture[] assignement isn't needed but I rather did it
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
        MaxAnisotropy[0] = 16;

        VertexShader = compile vs_3_0 VertexShaderFunction_DS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DS();
    }
}

technique Technique_RenderQualityExtreme
{
    pass Pass1
    {
        Texture[0] = <TextureDiffuseAndSpecular>;			//	I think this Texture[] assignement isn't needed but I rather did it
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
        MaxAnisotropy[0] = 16;

        VertexShader = compile vs_3_0 VertexShaderFunction_DS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DS();
    }
}