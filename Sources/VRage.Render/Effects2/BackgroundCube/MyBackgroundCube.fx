#include "../MyEffectBase.fxh"

float4x4 ViewProjectionMatrix;
float3 BackgroundColor = float3(1,1,1);

Texture BackgroundTexture;
sampler BackgroundTextureSampler = sampler_state 
{ 
    texture = <BackgroundTexture> ; 
    magfilter = LINEAR; 
    minfilter = LINEAR; 
    mipfilter = LINEAR; 
    AddressU = CLAMP; 
    AddressV = CLAMP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 TexCoord : TEXCOORD0; 
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 TexCoord : TEXCOORD0;    
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, ViewProjectionMatrix);
    output.TexCoord = input.TexCoord;
    
    // Cube texture faces are swaped (up/down, left/right, front/back - that's reason for minus sign)
    //output.TexCoord = -input.Position.xyz / input.Position.w;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 resultColor = texCUBE(BackgroundTextureSampler, input.TexCoord);
	resultColor.xyz *= BackgroundColor;
    
    return resultColor;
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
