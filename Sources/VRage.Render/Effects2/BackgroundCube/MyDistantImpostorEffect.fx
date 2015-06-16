#include "../MyEffectBase.fxh"
#include "../MyPerlinNoise.fxh"

float4x4 ViewProjectionMatrix;
float4x4 WorldMatrix;
float3 CameraPos;
float4 Animation;
float2 ContrastAndIntensity;
float3 Color;
float3 SunDir;

float Scale;

Texture ImpostorTexture;
sampler ImpostorTextureSampler = sampler_state 
{ 
	texture = <ImpostorTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = WRAP; 
	AddressV = WRAP;
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

struct VertexShaderInputColored
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutputColored
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutputTex3D
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
	float3 WorldPos : TEXCOORD1;
};



VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = mul(mul(input.Position, WorldMatrix), ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;

    return output;
}

VertexShaderOutputColored VertexShaderFunctionColoredLit(VertexShaderInputColored input)
{
    VertexShaderOutputColored output;

	float4 worldPos = mul(input.Position, WorldMatrix);
    output.Position = mul(worldPos, ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;

	//sundir must be there
	float sunLight = max(0, dot(SunDir, normalize(worldPos.xyz)));

    output.Color = input.Color;
	output.Color.xyz *= sunLight;

    return output;
}

VertexShaderOutputColored VertexShaderFunctionColored(VertexShaderInputColored input)
{
    VertexShaderOutputColored output;

	float4 worldPos = mul(input.Position, WorldMatrix);
    output.Position = mul(worldPos, ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;

    output.Color = input.Color;

    return output;
}

VertexShaderOutputTex3D VertexShaderFunctionTex3D(VertexShaderInput input)
{
    VertexShaderOutputTex3D output;

	float4 worldPos = mul(input.Position, WorldMatrix);

	output.WorldPos = worldPos.xyz;
    output.Position = mul(worldPos, ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;
	
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 resultColor = tex2D(ImpostorTextureSampler, input.TexCoord);
	return resultColor;
}

float4 PixelShaderFunctionColored(VertexShaderOutputColored input) : COLOR0
{
    float4 resultColor = tex2D(ImpostorTextureSampler, input.TexCoord) * input.Color;

	//resultColor.xyz = lerp(resultColor.xyz, FogColor, FogMultiplier);

	return resultColor;
}

float4 PixelShaderFunctionTex3D(VertexShaderOutputTex3D input) : COLOR0
{
	float f = noiseFogNebula(input.WorldPos, CameraPos, Scale, 0.000044f, Animation);

	f = pow(f, ContrastAndIntensity.x);
	f *= ContrastAndIntensity.y;

	return float4(Color, 1) * f;
}


technique Colored
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunctionColored();
        PixelShader = compile ps_2_0 PixelShaderFunctionColored();
    }
}

technique ColoredLit
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunctionColoredLit();
        PixelShader = compile ps_2_0 PixelShaderFunctionColored();
    }
}

technique Textured3D
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunctionTex3D();
        PixelShader = compile ps_3_0 PixelShaderFunctionTex3D();
    }
}


technique Default
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
