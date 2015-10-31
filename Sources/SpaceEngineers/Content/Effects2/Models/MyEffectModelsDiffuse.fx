#include "../MyEffectBase.fxh"

//	This shader renders a model with diffuse & specular & normal map textures, so it requires certain vertex shader data

float4x4	WorldMatrix;
float4x4	ViewMatrix;
float4x4	ProjectionMatrix;
float4	    DiffuseColor = float4(1,1,1,1); 

struct VertexShaderInputPositionColor
{
    float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutputPositionColor
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderInputPosition
{
    float4 Position : POSITION0;
};

struct VertexShaderOutputPosition
{
    float4 Position : POSITION0;
};


VertexShaderOutputPositionColor VertexShaderFunction1(VertexShaderInputPositionColor input)
{
    VertexShaderOutputPositionColor output;

	input.Position = UnpackPositionAndScale(input.Position);
	output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
    output.Position = mul(output.Position, ProjectionMatrix);    
    output.Color = input.Color;
    return output;
}

float4 PixelShaderFunction1(VertexShaderOutputPositionColor input) : COLOR0
{
	return input.Color * DiffuseColor;
}

VertexShaderOutputPosition VertexShaderFunction2(VertexShaderInputPosition input)
{
    VertexShaderOutputPosition output;

	input.Position = UnpackPositionAndScale(input.Position);
	output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
    output.Position = mul(output.Position, ProjectionMatrix);    
    return output;
}

float4 PixelShaderFunction2(VertexShaderOutputPosition input) : COLOR0
{
	return DiffuseColor.xyzw;
}


technique Technique_PositionColor
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction1();
        PixelShader = compile ps_3_0 PixelShaderFunction1();
    }
}

technique Technique_Position
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction2();
        PixelShader = compile ps_3_0 PixelShaderFunction2();
    }
}

