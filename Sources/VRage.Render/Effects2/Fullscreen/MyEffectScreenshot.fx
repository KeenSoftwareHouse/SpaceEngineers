#include "../MyEffectBase.fxh"

float2 HalfPixel;

//	Texture contains scene from LOD0
Texture SourceTexture;
sampler SourceTextureSampler = sampler_state 
{ 
	texture = <SourceTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

sampler SourceTextureLinearSampler = sampler_state 
{ 
	texture = <SourceTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};


float2 Scale = float2(1.0f, 1.0f);

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
	output.TexCoord = (input.TexCoord + HalfPixel) * Scale;
	return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(SourceTextureSampler, input.TexCoord);

	//diffuseColor = float4(1,0,0,1);
	
	return diffuseColor;
}

float4 PixelShaderFunctionLinear(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(SourceTextureLinearSampler, input.TexCoord);

	//diffuseColor = float4(1,0,0,1);
	
	return diffuseColor;
}

float4 PixelShaderFunctionColor(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(SourceTextureSampler, input.TexCoord);
	diffuseColor.a = 1;
	
	return diffuseColor;
}
float4 PixelShaderFunctionColorizeTexture(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = ColorizeTexture(tex2D(SourceTextureSampler, input.TexCoord),ColorMaskHSV);

	diffuseColor.a = 1;
	
	return diffuseColor;
}
float4 PixelShaderFunctionHDR(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(SourceTextureSampler, input.TexCoord);
	diffuseColor = Decode1010102(diffuseColor);
	diffuseColor.a = 1;
	
	return diffuseColor;
}

float4 PixelShaderFunctionAlpha(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(SourceTextureSampler, input.TexCoord);
	
	return float4(diffuseColor.www, 1);
}

float4 PixelShaderFunctionDepthToAlpha(VertexShaderOutput input) : COLOR0
{
	float4 depthColor = tex2D(SourceTextureSampler, input.TexCoord);
	float depth = DecodeFloatRGBA(depthColor);
	
	return float4(0,0,1, depth);
}

technique BasicTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}

technique ColorTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionColor();
	}
}

technique HDRTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionHDR();
	}
}

technique AlphaTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionAlpha();
	}
}

technique DepthToAlphaTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionDepthToAlpha();
	}
}

technique LinearTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionLinear();
	}
}

technique ColorizeTextureTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionColorizeTexture();
	}
}