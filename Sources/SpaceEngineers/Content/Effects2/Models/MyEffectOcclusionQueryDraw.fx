#include "../MyEffectBase.fxh"

float4x4	WorldMatrix;
float4x4	ViewMatrix;
float4x4	ProjectionMatrix;

float2 HalfPixel;
float2 Scale;

Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//	Here begins single-material rendering part
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float4 ScreenPosition : TEXCOORD0;
	float4 ViewPosition: TEXCOORD1;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	float2 texCoord = Scale * GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);
	float4 encodedDepth = tex2D(DepthsRTSampler, texCoord);
	float3 viewDir = input.ViewPosition.xyz;

	float viewDistance = length(input.ViewPosition.xyz);

	float depth = GetViewDistanceFromDepth(DecodeFloatRGBA(encodedDepth), viewDir);

	if(viewDistance > depth)
	{
		discard;
	}

	float3 normal = float3(0.0,1.0,0.0);
	float3 diffuse = float3(0.1f,0.1f,0.1f);

	MyGbufferPixelShaderOutput output;
	output = GetGbufferPixelShaderOutput(normal, diffuse, viewDistance);
	output.DiffuseAndSpecIntensity.a = 0.5f;
	return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4 PixelShaderFunctionNoDepthTest(VertexShaderOutput input) : COLOR
{
	return float4(0,1,0,1);
}


float4 PixelShaderFunctionNonMRT(VertexShaderOutput input): COLOR
{
	float2 texCoord = Scale * GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);
	float4 encodedDepth = tex2D(DepthsRTSampler, texCoord);
	float3 viewDir = input.ViewPosition.xyz;

	float viewDistance = length(input.ViewPosition.xyz);

	float depth = GetViewDistanceFromDepth(DecodeFloatRGBA(encodedDepth), viewDir);

	if(viewDistance > depth)
	{
		discard;
		return float4(1,0,0,1);
	}

	return float4(0,1,0,1);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4 PixelShaderFunctionNoDepthTestNonMRT(VertexShaderOutput input) : COLOR
{
	return float4(0,1,0,1);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
	output.ViewPosition = output.Position;
    output.Position = mul(output.Position, ProjectionMatrix); 
	output.ScreenPosition = output.Position;   
    return output;
}

technique EnableDepthTest
{
    pass Pass1
    {		
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}


technique DisableDepthTest
{
    pass Pass1
    {		
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunctionNoDepthTest();
    }
}


technique EnableDepthTestNonMRT
{
    pass Pass1
    {		
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunctionNonMRT();
    }
}


technique DisableDepthTestNonMRT
{
    pass Pass1
    {		
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunctionNoDepthTestNonMRT();
    }
}
