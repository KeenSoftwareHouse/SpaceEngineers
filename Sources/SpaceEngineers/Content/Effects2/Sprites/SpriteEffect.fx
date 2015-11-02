//-----------------------------------------------------------------------------
// SpriteEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"
#include "../MyEffectBase.fxh"


DECLARE_TEXTURE(Texture, 0);

TextureCube SpriteTextureCube;
sampler SpriteTextureSamplerCube = sampler_state 
{ 
	texture = <SpriteTextureCube> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};


BEGIN_CONSTANTS
MATRIX_CONSTANTS

    float4x4 MatrixTransform ;

END_CONSTANTS


void SpriteVertexShader(inout float4 position : POSITION,
						inout float4 color    : COLOR0,
                        inout float2 texCoord : TEXCOORD0)
{
    position = mul(position, MatrixTransform);
}


float4 SpritePixelShader(float4 color : COLOR0,	 float2 texCoord : TEXCOORD0) : COLOR0
{
  return SAMPLE_TEXTURE(Texture, texCoord) * color;
  //return float4(texCoord.x, texCoord.x, texCoord.x, 1) * color;
  //return color;
}

float4 SpritePixelShaderCube(float4 color : COLOR0,	 float2 texCoord : TEXCOORD0, uniform int faceIndex) : COLOR0
{
	float2 c = (texCoord - 0.5f) * 2;

	float3 coord;

	if(faceIndex == 0) coord = float3(1, -c.y, -c.x);
	else if(faceIndex == 1) coord = float3(-1, -c.y, c.x);
	else if(faceIndex == 2) coord = float3(c.x, 1, c.y);
	else if(faceIndex == 3) coord = float3(c.x, -1, -c.y);
	else if(faceIndex == 4) coord = float3(c.x, -c.y, 1.0f);
	else coord = float3(-c.x, -c.y, -1.0f);

    return texCUBE(SpriteTextureSamplerCube, coord) * color;    
}


technique SpriteBatch
{
    pass
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 SpritePixelShader();
    }
}

technique SpriteBatchCube0
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(0);
    }
}

technique SpriteBatchCube1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(1);
    }
}

technique SpriteBatchCube2
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(2);
    }
}

technique SpriteBatchCube3
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(3);
    }
}

technique SpriteBatchCube4
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(4);
    }
}

technique SpriteBatchCube5
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShaderCube(5);
    }
}
