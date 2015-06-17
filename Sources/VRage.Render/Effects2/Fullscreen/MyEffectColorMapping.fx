#include "../MyEffectBase.fxh"

float2 HalfPixel;
Texture InputTexture;

static const float3x3 TRANSFORM_SEPIA =
{
    0.393f, 0.769f, 0.189f,
    0.349f, 0.686f, 0.168f,
    0.272f, 0.534f, 0.131f,
};

static const float3 LUMINANCE = {0.3086f, 0.6094f, 0.0820f};
static const float3x3 TRANSFORM_LUMINANCE =
{
    LUMINANCE.r, LUMINANCE.g, LUMINANCE.b,
    LUMINANCE.r, LUMINANCE.g, LUMINANCE.b,
    LUMINANCE.r, LUMINANCE.g, LUMINANCE.b,
};

static const float3x3 IDENTITY =
{
    1.0f, 0.0f, 0.0f,
    0.0f, 1.0f, 0.0f,
    0.0f, 0.0f, 1.0f,
};

float3x3 ColorTransform = TRANSFORM_SEPIA;

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

float4 entryPS_ColorMappingEnabled(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(InputTextureSampler, input.TexCoord);
    color.rgb = mul(ColorTransform, color.rgb);

    return color;
}

float4 entryPS_ColorMappingDisabled(VertexShaderOutput input) : COLOR0
{
    return tex2D(InputTextureSampler, input.TexCoord);
}

//************************************************************************************

technique ColorMappingEnabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_ColorMappingEnabled();
    }
}

technique ColorMappingDisabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_ColorMappingDisabled();
    }
}
