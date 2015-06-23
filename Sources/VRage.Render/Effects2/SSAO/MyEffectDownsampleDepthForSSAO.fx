//	This shader runs on fullscreen quad and for each destination pixel finds four neighbourhood pixels in SourceDepthsRT
//	Then find the largest value and writes it into destination pixel.

#include "../MyEffectBase.fxh"

float2		HalfPixel;

Texture SourceDepthsRT;
sampler SourceDepthsRTSampler = sampler_state 
{ 
	texture = <SourceDepthsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
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
    output.TexCoord = input.TexCoord + 2 * HalfPixel;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{	
	//	We find values at four texels around our texel, and then later find the largest values (most distant distance)
	float leftTop = DecodeFloatRGBA(tex2D(SourceDepthsRTSampler, input.TexCoord + float2(-HalfPixel.x, -HalfPixel.y)));
	float rightTop = DecodeFloatRGBA(tex2D(SourceDepthsRTSampler, input.TexCoord + float2(+HalfPixel.x, -HalfPixel.y)));	
	float rightBottom = DecodeFloatRGBA(tex2D(SourceDepthsRTSampler, input.TexCoord + float2(+HalfPixel.x, +HalfPixel.y)));
	float leftBottom = DecodeFloatRGBA(tex2D(SourceDepthsRTSampler, input.TexCoord + float2(-HalfPixel.x, +HalfPixel.y)));	
	
	float minDistance = leftTop;
	minDistance = min(minDistance, rightTop);
	minDistance = min(minDistance, rightBottom);
	minDistance = min(minDistance, leftBottom);
	
	return EncodeFloatRGBA(minDistance);		
}

technique Technique1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}