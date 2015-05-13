#include "../MyEffectBase.fxh"

float SectorBorderWarningDistance;
float4 SectorBorderAditionalColor;
bool SectorBorderTabPressed;
float4x4 WorldMatrix;
float4x4 ViewProjectionMatrix;
float3 EyePosition;

Texture GridTexture;
sampler GridTextureSampler = sampler_state 
{ 
	texture = <GridTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 TexCoord : TEXCOORD0; 
    float4 InColor : COLOR;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 TexCoord : TEXCOORD0; 
    float3 WorldPosition		: TEXCOORD2; 
    float4 Color			: COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    float4 worldPosition = mul(input.Position, WorldMatrix);
    output.Position = mul(worldPosition, ViewProjectionMatrix);
    output.WorldPosition = worldPosition;
    output.TexCoord = input.TexCoord;
    output.Color = input.InColor;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	//	Our grid texture is stored only in 1 channel (for saving memory) so here we reconstruct it into all 4 channels
	float4 resultColor = tex2D(GridTextureSampler, input.TexCoord);
	resultColor = float4(resultColor.r, resultColor.r, resultColor.r, resultColor.r);

	resultColor.rgba *= SectorBorderAditionalColor.rgba;

	//	We display sector border in two cases: when user press TAB or when he approaches sector border
	//	Each case has different behaviour.
	if (SectorBorderTabPressed == false)
	{
		float dist = length(input.WorldPosition - EyePosition);
		float alphaByDistance = (1 - saturate(dist / SectorBorderWarningDistance));
		resultColor.a *= saturate(alphaByDistance * 2.5f);
	}

	//	Don't blend to full-fog-color, just little bit (we don't want to have farther objects same color)
	//	But don't change alpha
	//float4 fog = float4(FogColorForBackground.r, FogColorForBackground.g, FogColorForBackground.b, resultColor.a);
    //return lerp(resultColor, fog, FogMultiplier * 0.3);
	//return resultColor;
	return resultColor;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
