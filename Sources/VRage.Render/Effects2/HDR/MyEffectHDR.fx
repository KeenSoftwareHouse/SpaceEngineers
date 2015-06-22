#include "MyEffectHDRBase.fxh"

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

texture2D BloomTexture;
sampler2D BloomSampler = sampler_state
{
    Texture = <BloomTexture>;
    MinFilter = point;
    MagFilter = linear;
    MipFilter = NONE;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float4 ApplyToneMapAndBloomPS (	VertexShaderOutput input ) : COLOR0
{
	// Sample the original HDR image
	//float4 vSampleMod = tex2D(PointSamplerMod, input.TexCoord);
	//float4 vSampleDiv = tex2D(PointSamplerDiv, input.TexCoord);
	float4 vHDRColor = tex2D(PointSamplerMod, input.TexCoord);

	//vHDRColor = DecodeHDR(vSampleMod, vSampleDiv);
		
	// Do the tone-mapping
	float3 vToneMapped = ToneMap(vHDRColor.rgb);

	// Add in the bloom component
	float4 bloom = Decode1010102( tex2D(BloomSampler, input.TexCoord) );
	float3 vColor = vToneMapped + bloom.rgb;
	
	return float4(vColor, 1.0f);
}

technique BasicTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 ApplyToneMapAndBloomPS();
    }
}
