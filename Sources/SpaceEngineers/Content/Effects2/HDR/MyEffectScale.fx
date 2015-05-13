#include "MyEffectHDRBase.fxh"

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

texture2D SourceTextureDiv;
sampler2D LinearSamplerDiv = sampler_state
{
    Texture = <SourceTextureDiv>;
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float2 Scale = float2(1.0f, 1.0f);

VertexShaderOutput VertexShaderFunctionScale(VertexShaderInput input)
{
	//	We're using a full screen quad, no need for transforming vertices.
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = (input.TexCoord + HalfPixel) * Scale;
	return output;
}

/*
float2 SourceDimensions;

static const float Offsets[4] = {-1.5f, -0.5f, 0.5f, 1.5f};

// Downscales to 1/4 size, using 16 samples.
// Is currently not used.
float4 DownscalePS (	VertexShaderOutput input,
						uniform bool bDecodeLuminance	)	: COLOR0
{
	float4 vColor = 0;
	for (int x = 0; x < 4; x++)
	{
		for (int y = 0; y < 4; y++)
		{
			float2 vOffset;
			vOffset = float2(Offsets[x], Offsets[y]) / SourceDimensions;
			float4 vSample = tex2D(PointSampler, input.TexCoord + vOffset);
			vColor += vSample;
		}
	}

	vColor /= 16.0f;
		
	if (bDecodeLuminance)
		vColor = float4(exp(vColor.r), 1.0f, 1.0f, 1.0f);
	
	return vColor;
}
*/

// Upscales or downscales using hardware bilinear filtering
float4 HWScalePS ( VertexShaderOutput input )	: COLOR0
{
	return tex2D(LinearSampler, input.TexCoord);
}

// Downscales using level 3 mipmaps (and also decodes 2-RT HDR format and encodes to 1010102 format)
float4 Downscale8PS ( VertexShaderOutput input )	: COLOR0
{
	return Encode1010102( tex2Dlod(LinearSampler, float4(input.TexCoord, 0, 3.0f)));
}

// Downscales using level 2 mipmaps (and also decodes 2-RT HDR format and encodes to 1010102 format)
float4 Downscale4PS ( VertexShaderOutput input )	: COLOR0
{
	return Encode1010102( tex2Dlod(LinearSampler, float4(input.TexCoord, 0, 2.0f)));
}

technique HWScale
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 HWScalePS();
    }
}

technique HWScalePrefabPreviews
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunctionScale();
        PixelShader = compile ps_3_0 HWScalePS();
    }
}

/*
technique Downscale
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DownscalePS(false);
    }
}

technique DownscaleLuminance
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DownscalePS(true);
    }
}*/

technique Downscale4
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 Downscale4PS();
    }
}

technique Downscale8
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 Downscale8PS();
    }
}