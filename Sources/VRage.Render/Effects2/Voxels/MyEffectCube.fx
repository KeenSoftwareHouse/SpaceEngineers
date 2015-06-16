#include "MyEffectVoxelsBase.fxh"
#include "../MyPerlinNoise.fxh"

float4x4	ViewMatrix;
float4x4	WorldMatrix;
float4x4	ProjectionMatrix;
float3	    DiffuseColor;
float3	    Highlight = 0;
float3		BasePosition;

float EmissivityPower1;
float Time;

struct VertexShaderInputInstance
{
	float4 worldMatrixRow0 : BLENDWEIGHT0;
 	float4 worldMatrixRow1 : BLENDWEIGHT1;
 	float4 worldMatrixRow2 : BLENDWEIGHT2;
 	float4 worldMatrixRow3 : BLENDWEIGHT3;
	float4 diffuse : BLENDWEIGHT4;
	float4 SpecularIntensity_SpecularPower_Emisivity_HighlightFlag : BLENDWEIGHT5;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MyGbufferPixelShaderOutput PixelShaderFunction_Base(VertexShaderOutput input, float3 diffuse, float3 highlight, float3 si_sp_e, uniform int renderQuality)
{
	MyGbufferPixelShaderOutput output = GetTriplanarPixel(0, input.WorldPositionForTextureCoords, input.TriplanarWeights, input.Normal, input.ViewDistance.x, si_sp_e.x, si_sp_e.y, input.Ambient, renderQuality);
	output.DiffuseAndSpecIntensity.rgb = output.DiffuseAndSpecIntensity.rgb * diffuse + highlight;
	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection((1 - output.DepthAndEmissivity.a) - length(highlight), 0.0f); //Shader wont compile if I put there only length(highlight)...
	return output;
}

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input, uniform int renderQuality)
{
	//Cut pixels from LOD1 which are before LodNear
	if (IsPixelCut(input.ViewDistance.y))
	{
		discard;
		return (MyGbufferPixelShaderOutput)0;
		//return PixelShaderFunction_Base(input, float4(1,0,0,1), Highlight, float3(SpecularIntensity, SpecularPower, 0), renderQuality);
	}
	else					
		return PixelShaderFunction_Base(input, DiffuseColor, Highlight, float3(SpecularIntensity, SpecularPower, 0), renderQuality);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction_Base(VertexShaderInput input, float4x4 world)
{
	VertexShaderOutput output;

	float4 Position = UnpackPositionAndScale(input.PositionAndAmbient);
	float Ambient = 0;
	input.Normal = UnpackNormal(input.Normal);

	output.WorldPositionForTextureCoords = mul(Position.xyz, (float3x3)world) + BasePosition;
	output.WorldPositionForTextureCoords *= 10;
	float4 viewPosition = mul(Position, world);
	viewPosition = mul(viewPosition, ViewMatrix);

	//	We need distance between camera and the vertex. We don't want to use just Z, or Z/W, we just need that distance.	
	output.ViewDistance.x = -viewPosition.z;
	output.ViewDistance.y = length(viewPosition);
	
	
    output.Position = mul(viewPosition, ProjectionMatrix);        	
	//output.ViewDistance = output.Position.z;

    output.Normal = mul(input.Normal.xyz, (float3x3)world);
    output.TriplanarWeights = GetTriplanarWeights(output.Normal);
	output.Ambient = Ambient;
    return output;
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    return VertexShaderFunction_Base(input, WorldMatrix);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// FORWARD
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4 PixelShaderFunction_Forward(VertexShaderOutput input, uniform int renderQuality) : COLOR0
{
	MyGbufferPixelShaderOutput output = GetTriplanarPixel(0, input.WorldPositionForTextureCoords, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
	return float4(output.DiffuseAndSpecIntensity.rgb * input.Normal * DiffuseColor + Highlight, 1);
}

VertexShaderOutput VertexShaderFunction_Forward(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 Position = UnpackPositionAndScale(input.PositionAndAmbient);
	float Ambient = 0;
	input.Normal = UnpackNormal(input.Normal);

	output.WorldPositionForTextureCoords = mul(Position.xyz, (float3x3)WorldMatrix);
	float4 worldPosition = mul(Position, WorldMatrix);
	float4 viewPosition = mul(worldPosition, ViewMatrix);

	//	We need distance between camera and the vertex. We don't want to use just Z, or Z/W, we just need that distance.	
	output.ViewDistance.x = -viewPosition.z;
	output.ViewDistance.y = length(viewPosition);

	output.Position = mul(viewPosition, ProjectionMatrix);        	
	//output.ViewDistance = output.Position.z;

	output.Normal = mul(input.Normal.xyz, (float3x3)WorldMatrix);
	output.TriplanarWeights = GetTriplanarWeights(output.Normal);
// 	output.Ambient = Ambient;
// 	return output;

	// Forward lighting
	//float lightIntensity = ComputeForwardLighting(worldPosition, input.Normal);
	//output.Ambient = 0.1f /*+ saturate(0.3f * output.Ambient)*/ + 0.7f * saturate(abs(lightIntensity));

	float4 lightColor = 0;//ComputeForwardLighting(worldPosition, input.Normal);
	float ambient = 0.15f * float3(1,1,1);
	output.Normal = lightColor.xyz + ambient;
	output.Ambient = 0;

	return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityLow_Forward
{
	pass Pass1
	{
		DECLARE_TEXTURES_QUALITY_LOW

		VertexShader = compile vs_3_0 VertexShaderFunction_Forward();
		PixelShader = compile ps_3_0 PixelShaderFunction_Forward(RENDER_QUALITY_LOW);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityLow
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_LOW

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction(RENDER_QUALITY_LOW);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityNormal
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_NORMAL

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction(RENDER_QUALITY_NORMAL);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityHigh
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_HIGH

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction(RENDER_QUALITY_HIGH);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityExtreme
{
    pass Pass1
    {		
        DECLARE_TEXTURES_QUALITY_EXTREME
        
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction(RENDER_QUALITY_EXTREME);
    }
}
