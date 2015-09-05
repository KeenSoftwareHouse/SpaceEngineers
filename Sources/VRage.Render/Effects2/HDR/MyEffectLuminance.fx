#include "MyEffectHDRBase.fxh"

float DT;
float Tau = 0.5f;
float MipLevel;

texture2D SourceTexture;
sampler2D LinearSampler = sampler_state
{
    Texture = <SourceTexture>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
    AddressU = CLAMP;
    AddressV = CLAMP;
};
sampler2D PointSampler = sampler_state
{
    Texture = <SourceTexture>;
    MinFilter = point;
    MagFilter = point;
    MipFilter = none;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

texture2D SourceTexture2;
sampler2D PointSampler2 = sampler_state
{
    Texture = <SourceTexture2>;
    MinFilter = point;
    MagFilter = point;
    MipFilter = none;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float4 LuminancePS ( VertexShaderOutput input )	: COLOR0
{						
    float4 vSample = tex2D(LinearSampler, input.TexCoord);
    float3 vColor;
	vColor = Decode1010102(vSample);
	
    // calculate the luminance using a weighted average
    float Luminance = ConvertRGBToGray(vColor);
                
    float LogLuminance = log(1e-5 + Luminance); 
	//float LogLuminance = Luminance;
        
    // Output the luminance to the render target
    //return float4(LogLuminance, 1.0f, 0.0f, 0.0f);
	return float4(LogLuminance, LogLuminance, LogLuminance, 1.0f);
}

float4 LuminanceMipmapPS ( VertexShaderOutput input )	: COLOR0
{
	float logLum = tex2Dlod(LinearSampler, float4(input.TexCoord, 0, MipLevel)).r;
	//return tex2Dlod(LinearSampler, float4(input.TexCoord, 0, MipLevel));
	float tmp = exp(logLum);
	//float tmp = logLum;
	return float4(tmp, tmp, tmp, 1.0f);
}

float4 CalcAdaptedLumPS (VertexShaderOutput input)	: COLOR0
{
	float fLastLum = tex2D(PointSampler2, float2(0.5f, 0.5f)).r;
    float fCurrentLum = tex2D(PointSampler, float2(0.5f, 0.5f)).r;
    
    // Adapt the luminance using Pattanaik's technique
    float fAdaptedLum = fLastLum + (fCurrentLum - fLastLum) * (1 - exp(-DT * Tau));
    
    return float4(fAdaptedLum, fAdaptedLum, fAdaptedLum, 1.0f);
}

technique Luminance
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 LuminancePS();
    }
}

technique LuminanceMipmap
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 LuminanceMipmapPS();
    }
}

technique CalcAdaptedLuminance
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 CalcAdaptedLumPS();
    }
}
