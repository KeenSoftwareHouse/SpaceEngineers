#include "../MyEffectBase.fxh"

float2 HalfPixel;
Texture InputTexture;
float VignettingPower;

sampler InputTextureSampler = sampler_state 
{ 
    texture = <InputTexture> ; 
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = NONE;
    AddressU = clamp;
    AddressV = clamp;
    MaxAnisotropy = 4;
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

//************************************************************************************

VertexShaderOutput entryVS(VertexShaderInput input)
{
    //	We're using a full screen quad, no need for transforming vertices.
    VertexShaderOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord + HalfPixel;
    return output;
}

float4 entryPS_VignettingEnabled(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(InputTextureSampler, input.TexCoord);
    float2 dist = input.TexCoord - 0.5f;
    dist.x = 1 - dot(dist, dist);
    color.rgb *= saturate(pow(dist.x, VignettingPower));

    return color;
}

float4 entryPS_VignettingDisabled(VertexShaderOutput input) : COLOR0
{
    return tex2D(InputTextureSampler, input.TexCoord);
}

//************************************************************************************

technique VignettingEnabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_VignettingEnabled();
    }
}

technique VignettingDisabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_VignettingDisabled();
    }
}
