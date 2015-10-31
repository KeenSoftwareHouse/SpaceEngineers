#include "../MyEffectBase.fxh"

//	This shader renders a model with diffuse & specular & normal map textures, so it requires certain vertex shader data

float4x4	WorldViewProjectionMatrix;
float3	    DiffuseColor; 
float3		LIGHT_DIRECTION = normalize(float3(1.0f, 1.0f, -1.0f));
float4		AMBIENT_COLOR = float4(0.3f, 0.3f, 0.3f, 1);

struct VertexShaderInputGizmo
{
    float4 Position : POSITION0;
	float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
};

struct VertexShaderOutputGizmo
{
    float4 Position : POSITION0;
	float4 Color : TEXCOORD1;
};

VertexShaderOutputGizmo VertexShaderFunctionGizmo(VertexShaderInputGizmo input)
{
    VertexShaderOutputGizmo output;

	output.Position = mul(input.Position, WorldViewProjectionMatrix);
	float3 normal = normalize(mul(input.Normal, WorldViewProjectionMatrix));	
	output.Color = AMBIENT_COLOR + float4(DiffuseColor.xyz * dot(normal, LIGHT_DIRECTION), 1);
    return output;
}

float4 PixelShaderFunctionGizmo(VertexShaderOutputGizmo input) : COLOR0
{
	return input.Color;
}

technique Technique_RenderGizmo
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunctionGizmo();
        PixelShader = compile ps_3_0 PixelShaderFunctionGizmo();
    }
}


