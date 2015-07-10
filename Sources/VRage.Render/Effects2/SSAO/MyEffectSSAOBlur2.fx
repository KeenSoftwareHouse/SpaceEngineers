#include "../MyEffectBase.fxh"

float2 HalfPixel;
float2 BlurDirection;
float2 SSAOHalfPixel;
float samplesDiff;

texture DepthsRT;
sampler2D DepthsRTSampler = sampler_state
{
	Texture = <DepthsRT>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MAGFILTER = POINT;
	MINFILTER = POINT;
};

texture SsaoRT;
sampler2D SsaoRTSampler = sampler_state
{
	Texture = <SsaoRT>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MAGFILTER = POINT;
	MINFILTER = POINT;
	MIPFILTER = NONE;
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
    VertexShaderOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord;
    return output;
}

float4	SSAOBlur4EmbeddedDepth(
											float2 uv0: TEXCOORD0,
											float2 uv1: TEXCOORD1,
											float2 uv2: TEXCOORD2,
											float2 uv3: TEXCOORD3,
											float2 uv4: TEXCOORD4)
{
	float4	sample[5];
	float4	depths;
	float4	depths2 = 0;

	sample[0] = tex2D(SsaoRTSampler, uv0 + SSAOHalfPixel);// 'pivot' sample
	sample[1] = tex2D(SsaoRTSampler, uv1 + SSAOHalfPixel);
	sample[2] = tex2D(SsaoRTSampler, uv2 + SSAOHalfPixel);
	sample[3] = tex2D(SsaoRTSampler, uv3 + SSAOHalfPixel);
	sample[4] = tex2D(SsaoRTSampler, uv4 + SSAOHalfPixel);

	depths.x = DecodeFloatRGBA(tex2D(DepthsRTSampler, uv0 + HalfPixel)); 
	depths.y = DecodeFloatRGBA(tex2D(DepthsRTSampler, uv1 + HalfPixel)); 
	depths.z = DecodeFloatRGBA(tex2D(DepthsRTSampler, uv2 + HalfPixel)); 
	depths.w = DecodeFloatRGBA(tex2D(DepthsRTSampler, uv3 + HalfPixel)); 
	depths2.x = DecodeFloatRGBA(tex2D(DepthsRTSampler, uv4 + HalfPixel)); 

	float4	diff			= 8 * (float(1).xxxx - depths / depths.x);
	float4	weights		= saturate(float(0.5).xxxx - 0.75 * abs(diff) - 0.25 * diff);
	float		sumWeight	= weights.x + weights.y + weights.z + weights.w;
	float4		sumColor;

	/*
	sumColor  = sample[0] * weights.x;
	sumColor += sample[1] * weights.y;
	sumColor += sample[2] * weights.z;
	sumColor += sample[3] * weights.w;
	  */

	sumColor  = sample[0];
	sumColor += sample[1];
	sumColor += sample[2];
	sumColor += sample[3];
	sumColor += sample[4];

	//return saturate(sumColor / sumWeight);
	return saturate(sumColor / 5);
}


float4 PixelShaderFunction(float2 TexCoord :TEXCOORD0) : COLOR0
{														 /*
	float2 uv0 = TexCoord + float2(BlurDirection.x, 0); 
	float2 uv1 = TexCoord - float2(BlurDirection.x, 0);
	float2 uv2 = TexCoord + float2(0, BlurDirection.y);
	float2 uv3 = TexCoord - float2(0, BlurDirection.y);*/

	float2 uv0 = TexCoord; 
	float2 uv1 = TexCoord - BlurDirection;
	float2 uv2 = TexCoord + BlurDirection;
	float2 uv3 = TexCoord - 2*BlurDirection;
	float2 uv4 = TexCoord + 2*BlurDirection;

	return SSAOBlur4EmbeddedDepth(uv0, uv1, uv2, uv3, uv4);
	//return tex2D(SsaoRTSampler, uv1/ + SSAOHalfPixel*/);
}

technique SSAOBlur
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}