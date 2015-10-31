/*
    Cubic Lens Distortion HLSL Shader
    
    Original Lens Distortion Algorithm from SSontech (Syntheyes)
    http://www.ssontech.com/content/lensalg.htm
    
    r2 = image_aspect*image_aspect*u*u + v*v
    f = 1 + r2*(k + kcube*sqrt(r2))
    u' = f*u
    v' = f*v

    author : François Tarlier
    modified : Martin Kroslak
    website : www.francois-tarlier.com/blog/index.php/2009/11/cubic-lens-distortion-shader
*/

#include "../MyEffectBase.fxh"

Texture InputTexture;
float2 HalfPixel;
float AspectRatio;
float DistortionLens;
float DistortionCubic;
float3 DistortionWeights;

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

float2 distortTexCoords(float factor, float aspectRatio, float2 centeredCoords)
{
    float2 res = factor * centeredCoords;
    res.y *= aspectRatio;
    res += 0.5f;
    return res;
}

float4 entryPS_ChromaAberrationEnabled(VertexShaderOutput input) : COLOR0
{
    // Texture coordinates in range <-0.5, 0.5>
    float2 centeredTexCoords = input.TexCoord - 0.5f; 
    centeredTexCoords.y /= AspectRatio;

    float centerDistSqr = dot(centeredTexCoords, centeredTexCoords);	
    float3 distortFactor = 0;

    float3 tmp = DistortionLens * DistortionWeights;
    if (DistortionCubic != 0.0f)
        tmp += DistortionCubic * sqrt(centerDistSqr) * DistortionWeights;

    distortFactor = centerDistSqr * tmp + 1;

    // get the right pixel for the current position
    float4 resColor = float4(0,0,0,1);
    float2 newTexCoords;

    newTexCoords = distortTexCoords(distortFactor.r, AspectRatio, centeredTexCoords);
    resColor.r = tex2D(InputTextureSampler, newTexCoords).r;

    newTexCoords = distortTexCoords(distortFactor.g, AspectRatio, centeredTexCoords);
    resColor.g = tex2D(InputTextureSampler, newTexCoords).g;

    newTexCoords = distortTexCoords(distortFactor.b, AspectRatio, centeredTexCoords);
    resColor.b = tex2D(InputTextureSampler, newTexCoords).b;

    return resColor;
}

float4 entryPS_ChromaAberrationDisabled(VertexShaderOutput input) : COLOR0
{
    return tex2D(InputTextureSampler, input.TexCoord);
}

//************************************************************************************

technique TechniqueEnabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_ChromaAberrationEnabled();
    }
}

technique TechniqueDisabled
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 entryVS();
        PixelShader  = compile ps_3_0 entryPS_ChromaAberrationDisabled();
    }
}
