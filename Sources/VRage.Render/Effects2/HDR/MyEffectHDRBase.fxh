#include "../MyEffectBase.fxh"

static const float MiddleGrey = 0.9f; // not used right now
static const float MaxLuminance = 4.0f; // because of RGBA1010102
float Exposure = 1.85f;

float2 HalfPixel;

texture2D LumTexture;
sampler2D LumSampler = sampler_state
{
    Texture = <LumTexture>;
    MinFilter = point;
    MagFilter = point;
    MipFilter = none;
    AddressU = CLAMP;
    AddressV = CLAMP;
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
	output.TexCoord = input.TexCoord + HalfPixel;
	return output;
}

float3 ToneMap(float3 vColor)
{
	// Perform tone-mapping
	float Y = ConvertRGBToGray(vColor.rgb);
	float YD = Exposure * (Exposure/MaxLuminance + 1.0f) / (Exposure + 1.0f);
	vColor *= YD;
	return vColor;

	// ----- other tone mapping operator: -----
	/*
	// Get the calculated average luminance 
	float fLumAvg = tex2D(LumSampler, float2(0.5f, 0.5f)).r;	

	// Calculate the luminance of the current pixel
	float fLumPixel = ConvertRGBToGray(vColor);	
	
	// Apply the modified operator (Eq. 4)
	float fLumScaled = (fLumPixel * MiddleGrey) / fLumAvg;	
	float fLumCompressed = (fLumScaled * (1 + (fLumScaled / (MaxLuminance * MaxLuminance)))) / (1 + fLumScaled);
	return fLumCompressed * vColor;
	*/
}