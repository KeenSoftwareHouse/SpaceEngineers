#include "../MyEffectBase.fxh"

float2 HalfPixel;
float2 Scale;
float3 AmbientColor = float3(0.0f, 0.0f, 0.0f);

//	Texture contains scene from LOD0
Texture DiffuseTexture;
sampler DiffuseTextureSampler = sampler_state 
{ 
	texture = <DiffuseTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture LightTexture;
sampler LightTextureSampler = sampler_state 
{ 
	texture = <LightTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture LightTextureMod;
sampler LightTextureModSampler = sampler_state 
{ 
	texture = <LightTextureMod> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture LightTextureDiv;
sampler LightTextureDivSampler = sampler_state 
{ 
	texture = <LightTextureDiv> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture DepthTexture;
sampler DepthTextureSampler = sampler_state 
{ 
	texture = <DepthTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture NormalsTexture;
sampler NormalsTextureSampler = sampler_state 
{ 
	texture = <NormalsTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture BackgroundTexture;
sampler BackgroundTextureSampler = sampler_state 
{ 
	texture = <BackgroundTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};



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

float4 PixelShaderFunctionLDR(VertexShaderOutput input) : COLOR0
{
	float4 color = tex2D(BackgroundTextureSampler, input.TexCoord);
	float4 background = color;
	//background.xyz = lerp(background.xyz, FogColor.xyz, FogMultiplier);
//	background.w = 0;

	float3 normal = GetNormalVectorFromRenderTarget(tex2D(NormalsTextureSampler, input.TexCoord).xyz);

	float blend = length(normal);

	background.w = 1 - blend;

	return background;
	//return float4(color.xyz, 1);
	//return float4(blend.xxx, 1);
	//return float4(normal.xyz, 1);
}

float4 PixelShaderFunctionDisable(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColor = tex2D(DiffuseTextureSampler, input.TexCoord);

	float4 background = tex2D(BackgroundTextureSampler, input.TexCoord);
	background.w = 0;

	float3 normal = GetNormalVectorFromRenderTarget(tex2D(NormalsTextureSampler, input.TexCoord).xyz);

	float blend = length(normal);

	float4 color = lerp(background, diffuseColor, blend);

	return color;
    //return float4(normal.xyz, 1);
}

float4 PixelShaderFunctionOnlyLights(VertexShaderOutput input) : COLOR0
{
	float4 light = tex2D(LightTextureSampler, input.TexCoord);
    return float4(light.rgb, 1);
}

float4 PixelShaderFunctionOnlySpecularIntensity(VertexShaderOutput input) : COLOR0
{
    float4 diffuse = tex2D(DiffuseTextureSampler, input.TexCoord);
	float specularIntensity = diffuse.a * SPECULAR_INTENSITY_RATIO;

	return float4(specularIntensity.xxx, 1);
}

float4 PixelShaderFunctionOnlySpecularPower(VertexShaderOutput input) : COLOR0
{
	float4 normal = tex2D(NormalsTextureSampler, input.TexCoord);
	float specularPower = normal.a;

	return float4(specularPower.xxx, 1);
}

float4 PixelShaderFunctionOnlyEmissivity(VertexShaderOutput input) : COLOR0
{
	float4 depth = tex2D(DepthTextureSampler, input.TexCoord);
	float emissivity = UnpackGBufferEmissivity(depth.a);

	return float4(emissivity.xxx, 1);
}

float4 PixelShaderFunctionOnlyReflectivity(VertexShaderOutput input) : COLOR0
{
	float4 depth = tex2D(DepthTextureSampler, input.TexCoord);
	float reflectivity = UnpackGBufferReflection(depth.a);

	return float4(reflectivity.xxx, 1);
}

float4 PixelShaderFunctionCopyEmissivity(VertexShaderOutput input) : COLOR0
{
	//float4 diffuseColor = tex2D(DiffuseTextureSampler, input.TexCoord);
	
	float4 encodedDepth = tex2D(DepthTextureSampler, input.TexCoord);
	float4 diffuse = tex2D(DiffuseTextureSampler, input.TexCoord);
	float4 background = tex2D(BackgroundTextureSampler, input.TexCoord);

	float3 normal = GetNormalVectorFromRenderTarget(tex2D(NormalsTextureSampler, input.TexCoord).xyz);
	float blend = 1 - length(normal);

	float emissive = UnpackGBufferEmissivity(encodedDepth.w);
	emissive *= 1 + FogMultiplier * FogMultiplier * blend;
	return float4(diffuse.xyz * emissive.xxx, emissive + background.w * blend * FogBacklightMultiplier);

	//return float4(1,1,1,0);
	
	//return float4(diffuse * emissive.xxx, emissive / (1 - FogMultiplier * blend / 4));
	//return float4(diffuse * emissive.xxx, emissive + emissive * FogMultiplier * FogMultiplier * blend);
	//float4(diffuseColor * emissiveReal.xxx,1)
}

technique BasicTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionLDR();
	}
}

technique DisableLightsTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionDisable();
	}
}

technique OnlyLightsTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOnlyLights();
	}
}

technique OnlySpecularIntensity
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOnlySpecularIntensity();
	}
}

technique OnlySpecularPower
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOnlySpecularPower();
	}
}

technique OnlyEmissivity
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOnlyEmissivity();
	}
}

technique OnlyReflectivity
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionOnlyReflectivity();
	}
}

technique CopyEmissivity
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionCopyEmissivity();
	}
}