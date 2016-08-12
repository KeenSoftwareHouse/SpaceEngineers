#include "../Voxels/MyEffectVoxelsBase.fxh"

float4x4    WorldMatrix;

#include "MyEffectVoxelVertex.fxh"

//	This shader renders a model with same shader as voxels (triplanar mapping), using diffuse & specular & normal map textures
//	But we use one texture-set for XZ plane, and the other for Y plan

float4x4	ViewWorldScaleMatrix;
float4x4	ProjectionMatrix;

//  This applies only for explosion debris, because we want to add some randomization to 'world position to texture coord' transformation
float		TextureCoordRandomPositionOffset;
float		TextureCoordScale;

//	Add random color overlay on explosion debris diffuse texture output
float		DiffuseTextureColorMultiplier;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input, uniform int renderQuality)
{
    float3 localPosition = VoxelVertex_CellRelativeToLocalPosition(input.CellRelativePosition);
	MyGbufferPixelShaderOutput output = GetTriplanarPixel(0, localPosition, input.TriplanarWeights, input.Normal, input.ViewDistance, SpecularIntensity, SpecularPower, input.Ambient, renderQuality);
	output.DiffuseAndSpecIntensity.rgb *= DiffuseTextureColorMultiplier;
	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection(1 - output.DepthAndEmissivity.w, 0.0f); //inverted emissivity, zero reflection
	return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	input.PositionAndAmbient = UnpackPositionAndScale(input.PositionAndAmbient);
	float4 Position = float4(input.PositionAndAmbient.xyz, 1);

	// In models .w component has 1.0 by default, we may use shader constant instead and set ambient per voxel material
	float Ambient = 0.0f;//input.PositionAndAmbient.w;
	input.Normal = UnpackNormal(input.Normal);

	//	Here we get texture coord based on object-space coordinates of model. The correct way would be to do this
	//	after ScaleMatrix multiplication, but this version is faster (one less mul in vertex shader) and it looks
	//	fine, I am going with this one.
	output.CellRelativePosition = (Position + TextureCoordRandomPositionOffset) * TextureCoordScale;
	float4 viewPosition = mul(Position, ViewWorldScaleMatrix);

	//	We need distance between camera and the vertex. We don't want to use just Z, or Z/W, we just need that distance.
	output.ViewDistance = -viewPosition.z;

    output.Position = mul(viewPosition, ProjectionMatrix);
    output.Normal = normalize(mul(input.Normal.xyz, (float3x3)WorldMatrix));
    output.TriplanarWeights = GetTriplanarWeights(output.Normal);
	output.Ambient = Ambient;
    return output;
}


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

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
