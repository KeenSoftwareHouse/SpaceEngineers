#include "MyEffectVoxelsBase.fxh"
float4x4 WorldMatrix;

#include "MyEffectVoxelVertex.fxh"
#include "../MyEffectAtmosphereBase.fxh"

float4x4 ViewMatrix;
float4x4 ProjectionMatrix;
float3   DiffuseColor;
float3   Highlight;
float EnableFog;
float3 PositionToLefBottomOffset;

bool HasAtmosphere;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#define DEBUG_TEX_COORDS 0

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input, uniform int renderQuality)
{
    float3 localPosition = VoxelVertex_CellRelativeToLocalPosition(input.CellRelativePosition);
    MyGbufferPixelShaderOutput output = GetTriplanarPixel(0, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
    output.DiffuseAndSpecIntensity.rgb = output.DiffuseAndSpecIntensity.rgb * DiffuseColor + Highlight;
    output.DepthAndEmissivity.a = PackGBufferEmissivityReflection((1 - output.DepthAndEmissivity.w) + length(Highlight), 0.0f); //inverted emissivity, zero reflection
#if DEBUG_TEX_COORDS
    output.DiffuseAndSpecIntensity = float4(frac(localPosition), 1.0f);
#endif
    return output;
}

MyGbufferPixelShaderOutput PixelShaderFunction_Multimaterial(VertexShaderOutput_Multimaterial multiInput, uniform int renderQuality)
{
    VertexShaderOutput input = multiInput.Single;

    float3 localPosition = VoxelVertex_CellRelativeToLocalPosition(input.CellRelativePosition);
    MyGbufferPixelShaderOutput output0 = GetTriplanarPixel(0, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
    MyGbufferPixelShaderOutput output1 = GetTriplanarPixel(1, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity2, SpecularPower2, input.Ambient, renderQuality);
    MyGbufferPixelShaderOutput output2 = GetTriplanarPixel(2, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity3, SpecularPower3, input.Ambient, renderQuality);

    float3 normal0 = GetNormalVectorFromRenderTarget(output0.NormalAndSpecPower.xyz);
    float3 normal1 = GetNormalVectorFromRenderTarget(output1.NormalAndSpecPower.xyz);
    float3 normal2 = GetNormalVectorFromRenderTarget(output2.NormalAndSpecPower.xyz);

    MyGbufferPixelShaderOutput output;
    output.NormalAndSpecPower.xyz = GetNormalVectorIntoRenderTarget(normalize(normal0 * multiInput.Alpha.x + normal1 * multiInput.Alpha.y + normal2 * multiInput.Alpha.z));
    output.NormalAndSpecPower.w = output0.NormalAndSpecPower.w * multiInput.Alpha.x + output1.NormalAndSpecPower.w * multiInput.Alpha.y + output2.NormalAndSpecPower.w * multiInput.Alpha.z;
    output.DiffuseAndSpecIntensity = output0.DiffuseAndSpecIntensity * multiInput.Alpha.x + output1.DiffuseAndSpecIntensity * multiInput.Alpha.y + output2.DiffuseAndSpecIntensity * multiInput.Alpha.z;
    output.DepthAndEmissivity = output0.DepthAndEmissivity * multiInput.Alpha.x + output1.DepthAndEmissivity * multiInput.Alpha.y + output2.DepthAndEmissivity * multiInput.Alpha.z;

    output.DiffuseAndSpecIntensity.rgb = output.DiffuseAndSpecIntensity.rgb * DiffuseColor + Highlight;
    output.DepthAndEmissivity.a = PackGBufferEmissivityReflection((1 - output.DepthAndEmissivity.w) + length(Highlight), 0.0f); //inverted emissivity, zero reflection

#if DEBUG_TEX_COORDS
    output.DiffuseAndSpecIntensity = float4(frac(localPosition), 1.0f);
#endif

    return output;
}

float4 CalculateLighting(VoxelPixelData pixelData,float viewDistance, float3 worldPosition)
{
	float3 normal = pixelData.Normal;
	float NdLbase = dot(normal, -LightDirection);
	float NdL = max(0,NdLbase);

    float backNdL = max(0,-NdLbase);
	float3 backDiffuseLight = backNdL * BacklightColorAndIntensity.xyz * pixelData.DiffuseTexture.rgb;

    float3 diffuseLight = LightColorAndIntensity.rgb*NdL  * pixelData.DiffuseTexture.rgb;

	float3 ambientTexCoord = -pixelData.Normal.xyz;
	float4 ambientSample = SampleAmbientTexture(ambientTexCoord);
	float3 ambientColor = AmbientMinimumAndIntensity.w * ambientSample.xyz;
	float3 finalAmbientColor =  max(ambientColor, AmbientMinimumAndIntensity.xyz) * pixelData.DiffuseTexture.rgb;


	float3 reflectionVector = -(reflect(-LightDirection, normal.xyz));
	float3 reflectionVectorBack = -(reflect(LightDirection, normal.xyz));
	float3 directionToCamera = normalize(-worldPosition.xyz);

	float3 specular = float3(0,0,0);
	float specularLight = 0;
	if (pixelData.SpecularIntensity > 0)
	{
		//compute specular light
		float specularLight = pixelData.SpecularIntensity * pow( saturate(dot(reflectionVector, directionToCamera)), pixelData.SpecularPower);
		specular = specularLight.xxx*LightSpecularColor;
	}

	float backSpecular = pixelData.SpecularIntensity * pow( saturate(dot(reflectionVectorBack, directionToCamera)), pixelData.SpecularPower) * 0.5f;
	backSpecular = backSpecular.xxx * float3(1,1,1) * lerp(float3(1,1,1), pixelData.DiffuseTexture.rgb, 0.5);

	float4 finalColor =  float4(LightColorAndIntensity.w * (diffuseLight + specular),1);
	finalColor += float4(finalAmbientColor + BacklightColorAndIntensity.w * (backDiffuseLight + backSpecular) * ambientSample.w, 1);

	float4 fogColor = CalculateFogLinear(viewDistance);
	pixelData.DiffuseTexture.rgb = lerp(finalColor,fogColor,fogColor.a*EnableFog);
	return pixelData.DiffuseTexture;
}


float4 PixelShaderFunctionFar(VertexShaderOutput input, uniform int renderQuality):COLOR0
{
    float3 localPosition = VoxelVertex_CellRelativeToLocalPosition(input.CellRelativePosition);
	VoxelPixelData pixelData = GetTriplanarPixelFar(0, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
	pixelData.DiffuseTexture.rgb = pixelData.DiffuseTexture.rgb* DiffuseColor + Highlight;

    float3 worldPosition = VoxelVertex_CellRelativeToWorldPosition(input.CellRelativePosition);
	float4 final = CalculateLighting(pixelData, input.ViewDistance, worldPosition);
	if (HasAtmosphere)
	{
		final = CalculateAtmosphere(localPosition, PositionToLefBottomOffset,final.rgb);
	}
	return final;
}

float4 PixelShaderFunctionFar_Multimaterial(VertexShaderOutput_Multimaterial multiInput, uniform int renderQuality):COLOR0
{
    VertexShaderOutput input = multiInput.Single;
    float3 localPosition = VoxelVertex_CellRelativeToLocalPosition(input.CellRelativePosition);
	VoxelPixelData pixelData0 = GetTriplanarPixelFar(0, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
	VoxelPixelData pixelData1 = GetTriplanarPixelFar(1, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity2, SpecularPower2, input.Ambient, renderQuality);
	VoxelPixelData pixelData2 = GetTriplanarPixelFar(2, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity3, SpecularPower3, input.Ambient, renderQuality);

	VoxelPixelData pixelData;
	pixelData.Normal = normalize(pixelData0.Normal * multiInput.Alpha.x + pixelData1.Normal* multiInput.Alpha.y + pixelData2.Normal * multiInput.Alpha.z);
	pixelData.DiffuseTexture = pixelData0.DiffuseTexture * multiInput.Alpha.x + pixelData1.DiffuseTexture* multiInput.Alpha.y + pixelData2.DiffuseTexture * multiInput.Alpha.z;
	pixelData.SpecularIntensity = pixelData0.SpecularIntensity * multiInput.Alpha.x + pixelData1.SpecularIntensity* multiInput.Alpha.y + pixelData2.SpecularIntensity * multiInput.Alpha.z;
	pixelData.SpecularPower = pixelData0.SpecularPower * multiInput.Alpha.x + pixelData1.SpecularPower* multiInput.Alpha.y + pixelData2.SpecularPower * multiInput.Alpha.z;

	pixelData.DiffuseTexture.rgb = pixelData.DiffuseTexture.rgb* DiffuseColor + Highlight;
    float3 worldPosition = VoxelVertex_CellRelativeToWorldPosition(input.CellRelativePosition);
	float4 final = CalculateLighting(pixelData, input.ViewDistance, worldPosition);
    if (HasAtmosphere)
	{
		final = CalculateAtmosphere(localPosition, PositionToLefBottomOffset, final.rgb);
	}
	return final;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void ReadCellRelativePosition(VertexShaderInput input, out float3 positionA, out float3 positionB)
{
    positionA = VoxelVertex_NormalizedToCellRelativePosition(input.PositionAndAmbient.xyz);
    positionB = VoxelVertex_NormalizedToCellRelativePosition(input.PositionMorph.xyz);
}

void ComputeCommonOutput(VertexShaderInput input, float3 cellRelativePosition, float3 normal, out VertexShaderOutput output)
{
    output.Ambient = UnpackVoxelAmbient(input.PositionAndAmbient);

    output.CellRelativePosition = cellRelativePosition;
    float3 worldPosition = VoxelVertex_CellRelativeToWorldPosition(cellRelativePosition);
    float4 viewPosition = mul(float4(worldPosition, 1), ViewMatrix);

    // We need distance between camera and the vertex. We don't want to use just Z, or Z/W, we just need that distance.
    output.ViewDistance = -viewPosition.z;

    output.Position = mul(viewPosition, ProjectionMatrix);
    output.Normal = normal;
    output.TriplanarWeights = GetTriplanarWeights(output.Normal);
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    float3 positionA, positionB, normalA, normalB;

    ReadCellRelativePosition(input, positionA, positionB);
    normalA = UnpackNormal(input.Normal);
    normalB = UnpackNormal(input.NormalMorph);

    float morph = VoxelVertex_ComputeMorphParameter(positionA);
    float3 position = lerp(positionA, positionB, morph);
    float3 normal = normalize(lerp(normalA, normalB, morph));

    VertexShaderOutput output;
    ComputeCommonOutput(input, position, normal, output);
    return output;
}

VertexShaderOutput_Multimaterial VertexShaderFunction_Multimaterial(VertexShaderInput input)
{
    float3 positionA, positionB, alphaA, alphaB, normalA, normalB;
    ReadCellRelativePosition(input, positionA, positionB);
    alphaA = UnpackVoxelAlpha(input.PositionAndAmbient);
    alphaB = UnpackVoxelAlpha(input.PositionMorph);
    normalA = UnpackNormal(input.Normal);
    normalB = UnpackNormal(input.NormalMorph);

    float morph = VoxelVertex_ComputeMorphParameter(positionA);
    float3 position = lerp(positionA, positionB, morph);
    float3 normal = normalize(lerp(normalA, normalB, morph));
    float3 alpha = lerp(alphaA, alphaB, morph);

    VertexShaderOutput_Multimaterial output;
    output.Alpha = alpha;
    ComputeCommonOutput(input, position, normal, output.Single);
    return output;
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

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityNormal_Multimaterial
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_NORMAL

        VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
        PixelShader = compile ps_3_0 PixelShaderFunction_Multimaterial(RENDER_QUALITY_NORMAL);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityHigh_Multimaterial
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_HIGH

        VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
        PixelShader = compile ps_3_0 PixelShaderFunction_Multimaterial(RENDER_QUALITY_HIGH);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityExtreme_Multimaterial
{
    pass Pass1
    {
        DECLARE_TEXTURES_QUALITY_EXTREME

        VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
        PixelShader = compile ps_3_0 PixelShaderFunction_Multimaterial(RENDER_QUALITY_EXTREME);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


technique Technique_RenderQualityNormal_Far
{
	pass Pass1
    {
		DECLARE_TEXTURES_QUALITY_NORMAL

        VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar(RENDER_QUALITY_NORMAL);
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityNormal_Mulitmaterial_Far
{
	pass Pass1
    {
		DECLARE_TEXTURES_QUALITY_NORMAL

        VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar_Multimaterial(RENDER_QUALITY_NORMAL);
    }
}

technique Technique_RenderQualityHigh_Far
{
	pass Pass1
	{
		DECLARE_TEXTURES_QUALITY_HIGH

		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar(RENDER_QUALITY_HIGH);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityHigh_Mulitmaterial_Far
{
	pass Pass1
	{
		DECLARE_TEXTURES_QUALITY_HIGH

		VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar_Multimaterial(RENDER_QUALITY_HIGH);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


technique Technique_RenderQualityExtreme_Far
{
	pass Pass1
	{
		DECLARE_TEXTURES_QUALITY_EXTREME

		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar(RENDER_QUALITY_EXTREME);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Technique_RenderQualityExtreme_Mulitmaterial_Far
{
	pass Pass1
	{
		DECLARE_TEXTURES_QUALITY_EXTREME

		VertexShader = compile vs_3_0 VertexShaderFunction_Multimaterial();
		PixelShader = compile ps_3_0 PixelShaderFunctionFar_Multimaterial(RENDER_QUALITY_EXTREME);
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
