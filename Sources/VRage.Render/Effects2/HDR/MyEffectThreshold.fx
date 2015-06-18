#include "MyEffectHDRBase.fxh"

float Threshold;
float BloomIntensity;
float BloomIntensityBackground;

texture2D SourceTexture;
sampler2D PointSamplerMod = sampler_state
{
	Texture = <SourceTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = none;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

texture2D SourceTextureDiv;
sampler2D PointSamplerDiv = sampler_state
{
	Texture = <SourceTextureDiv>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = none;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float4 ThresholdPS ( VertexShaderOutput input ): COLOR0
{
	//return float4(1,0,0, 1);
	
	// Sample the original HDR image
	//float4 vSampleMod = tex2D(PointSamplerMod, input.TexCoord);
	//float4 vSampleDiv = tex2D(PointSamplerDiv, input.TexCoord);
	
	//float4 vHDRColor = DecodeHDR(vSampleMod, vSampleDiv);
	float4 vHDRColor = tex2D(PointSamplerMod, input.TexCoord);

	// TODO remove ifs?


	//// most emissive parts
	//if (vHDRColor.a > 0.99f)
	//{
	//	vHDRColor.rgb = BloomIntensityBackground * vHDRColor.rgb;
	//	return float4(ToneMap(vHDRColor.rgb), 1.0f);
	//	//return float4(1,0,0, 1.0f);
	//}

	
	float luminance = ConvertRGBToGray(vHDRColor.rgb);

	// Usual emissivity
	if (vHDRColor.r > Threshold)
	{
		//vHDRColor *= vHDRColor.a;
		vHDRColor.rgb = BloomIntensity * luminance;
		return float4(ToneMap(vHDRColor.rgb), 1.0f);
		//return float4(BloomIntensity,0,0, 1.0f);
	}

	//return float4(vHDRColor.www, 1);
	
	//vHDRColor = float4(ToneMap(vHDRColor.rgb), 1.0f);

	//float luminance = ConvertRGBToGray(vHDRColor.rgb);
	//luminance -= Threshold;
	//luminance = max(luminance, 0.0f);
	//vHDRColor.rgb *= luminance;
	//
	//vHDRColor.rgb = BloomIntensity * vHDRColor.rgb;
	
	return float4(0,0,0,0);
	//return vHDRColor.b > 1.0? float4(0,0,0,0) : float4(0,1,0,1);
}

technique BasicTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 ThresholdPS();
	}
}