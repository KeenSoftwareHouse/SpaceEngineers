#include "../MyEffectShadowBase.fxh"
#include "../MyEffectInstanceBase.fxh"

float4x4	WorldMatrix;

#include "../Voxels/MyEffectVoxelVertex.fxh"

float4x4	ViewProjMatrix;
float2		ShadowTermHalfPixel;
float3		FrustumCornersVS[4];
float2		HalfPixel;

float4x4 Bones[SKINNED_EFFECT_MAX_BONES];

float		Dithering = 0;
float2		TextureDitheringSize;


texture DepthTexture;
sampler2D DepthTextureSampler = sampler_state
{
    Texture = <DepthTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = none;
};

Texture TextureDithering;
sampler TextureDitheringSampler = sampler_state
{
	texture = <TextureDithering> ;
	mipfilter = NONE;
	magfilter = POINT;
	minfilter = POINT;
	AddressU = WRAP;
	AddressV = WRAP;
};

float4 ComputeShadowMapValue(in float2 in_vDepthCS, in float2 in_vTex, in float in_dither)
{
	// Negate and divide by distance to far clip (so that depth is in range [0,1])
	float fDepth = in_vDepthCS.x / in_vDepthCS.y;
	//fDepth = -log2(fDepth) / 10;

	// Use ordered dithering, store numbers in constant buffer, it won't require texture anymore
	// http://en.wikipedia.org/wiki/Ordered_dithering
	if(in_dither > 0)
	{
		float2 pixelCount = 1 / (2 * HalfPixel);
		float2 tex = in_vTex.xy + HalfPixel;
		float2 p = tex / 2 + 1;

		float dither = tex2D(TextureDitheringSampler, p * pixelCount / TextureDitheringSize).x;

		if(dither < in_dither)
		{
			fDepth = 0;
		}
		else
		{
			discard;
		}
	}



#ifdef COLOR_SHADOW_MAP_FORMAT
	return EncodeFloatRGBAPrecise(fDepth);
#else
    return float4(fDepth, 0, 0, 0);
#endif
}

void GenerateShadowMapVS_Base(in float4 in_vPositionOS, in float4x4 worldMatrix, out float4 out_vPositionCS, out float2 out_vDepthCS, out float2 out_vTex, out float out_dither)
{
	// Unpack position
	in_vPositionOS = UnpackPositionAndScale(in_vPositionOS);

	// Figure out the position of the vertex in view space and clip space
	out_vPositionCS = mul(in_vPositionOS, worldMatrix);
    out_vPositionCS = mul(out_vPositionCS, ViewProjMatrix);
	out_vDepthCS = out_vPositionCS.zw;
	out_vTex = out_vPositionCS.xy;
	out_dither = Dithering;
}

// Vertex shader for outputting light-space depth to the shadow map
void GenerateShadowMapVS(	in float4 in_vPositionOS	: POSITION,
							out float4 out_vPositionCS	: POSITION,
							out float2 out_vDepthCS		: TEXCOORD0,
							out float2 out_vTex			: TEXCOORD1,
							out float out_dither : TEXCOORD2)
{
	GenerateShadowMapVS_Base(in_vPositionOS, WorldMatrix, out_vPositionCS, out_vDepthCS, out_vTex, out_dither);
}

void GenerateShadowMapVS_Instanced(	in float4 in_vPositionOS	: POSITION,
									in int4 Indices				: BLENDINDICES0,
									in float4 Weights			: BLENDWEIGHT0,
									in VertexShaderInput_InstanceData instanceData,
									out float4 out_vPositionCS	: POSITION,
									out float2 out_vDepthCS		: TEXCOORD0,
									out float2 out_vTex		: TEXCOORD1,
									out float out_dither : TEXCOORD2)
{
	float4 unpackedPos = UnpackPositionAndScale(in_vPositionOS);
	float4 instance_dither;
	float4x4 instanceWorldMatrix = GetInstanceMatrix(unpackedPos.xyz, Indices, Weights, instanceData, instance_dither);
	GenerateShadowMapVS_Base(in_vPositionOS, mul(instanceWorldMatrix, WorldMatrix), out_vPositionCS, out_vDepthCS, out_vTex, out_dither);
	out_dither = instance_dither.w;
}

void GenerateShadowMapVS_InstancedGeneric(	in float4 in_vPositionOS	: POSITION,
									in VertexShaderInput_GenericInstanceData instanceData,
									out float4 out_vPositionCS	: POSITION,
									out float2 out_vDepthCS		: TEXCOORD0,
									out float2 out_vTex		: TEXCOORD1,
									out float out_dither : TEXCOORD2)
{
	float4 unpackedPos = UnpackPositionAndScale(in_vPositionOS);
	matrix instanceMatrix = matrix(instanceData.matrix_row0, instanceData.matrix_row1, instanceData.matrix_row2, float4(0,0,0,1));
	instanceMatrix = transpose(instanceMatrix);
	GenerateShadowMapVS_Base(in_vPositionOS, mul(instanceMatrix, WorldMatrix), out_vPositionCS, out_vDepthCS, out_vTex, out_dither);
	out_dither = max(instanceData.colorMaskHSV.w, out_dither);
}

// Vertex shader for outputting light-space depth to the shadow map for voxels
void GenerateVoxelShadowMapVS(	in float4 in_vPositionOS	: POSITION,
								in float4 in_vPositionMorphOS	: POSITION1,
							out float4 out_vPositionCS	: POSITION,
							out float2 out_vDepthCS		: TEXCOORD0,
							out float2 out_vTex		: TEXCOORD1,
							out float out_dither : TEXCOORD2)
{
	float4 Position;
	{
		// Unpack voxel position
		float3 cellRelativeA = VoxelVertex_NormalizedToCellRelativePosition(in_vPositionOS.xyz);
		float3 cellRelativeB = VoxelVertex_NormalizedToCellRelativePosition(in_vPositionMorphOS.xyz);
		float morph = VoxelVertex_ComputeMorphParameter(cellRelativeA);
		float3 cellRelative = lerp(cellRelativeA, cellRelativeB, morph);
		Position = float4(VoxelVertex_CellRelativeToWorldPosition(cellRelative), 1);
	}

	out_vPositionCS = mul(Position, ViewProjMatrix);
	out_vDepthCS = out_vPositionCS.zw;
	out_vTex = out_vPositionCS.xy;
	out_dither = Dithering;
}

float4 GenerateVoxelShadowMapPS(
	in float2 in_vDepthCS : TEXCOORD0,
	in float2 in_vTex : TEXCOORD1,
	in float in_dither : TEXCOORD2) : COLOR0
{
	return ComputeShadowMapValue(in_vDepthCS, in_vTex, in_dither);
}

void GenerateShadowMapVS_Skinned(	in float4 in_vPositionOS	: POSITION,
							in int4 Indices:  BLENDINDICES0,
							in float4 Weights  : BLENDWEIGHT0,
							out float4 out_vPositionCS	: POSITION,
							out float2 out_vDepthCS		: TEXCOORD0,
							out float2 out_vTex		: TEXCOORD1,
							out float out_dither : TEXCOORD2)
{
	float4x4 world = 0;

    for (int i = 0; i < 4; i++)
    {
        world += Bones[Indices[i]] * Weights[i];
    }

	GenerateShadowMapVS_Base(in_vPositionOS, mul(world, WorldMatrix), out_vPositionCS, out_vDepthCS, out_vTex, out_dither);
}

// Pixel shader for outputting light-space depth to the shadow map
float4 GenerateShadowMapPS(in float2 in_vDepthCS : TEXCOORD0, in float2 in_vTex : TEXCOORD1, in float in_dither : TEXCOORD2) : COLOR0
{
    return ComputeShadowMapValue(in_vDepthCS, in_vTex, in_dither);
}

// Vertex shader for rendering the full-screen quad used for calculating
// the shadow occlusion factor.
void ShadowTermVS (	in float3 in_vPositionOS				: POSITION,
					in float3 in_vTexCoordAndCornerIndex	: TEXCOORD0,
					out float4 out_vPositionCS				: POSITION,
					out float2 out_vTexCoord				: TEXCOORD0,
					out float3 out_vFrustumCornerVS			: TEXCOORD1	)
{
	// Offset the position by half a pixel to correctly align texels to pixels
	out_vPositionCS.x = in_vPositionOS.x - ShadowTermHalfPixel.x;
	out_vPositionCS.y = in_vPositionOS.y + ShadowTermHalfPixel.y;
	out_vPositionCS.z = in_vPositionOS.z;
	out_vPositionCS.w = 1.0f;

	// Pass along the texture coordiante and the position of the frustum corner
	out_vTexCoord = in_vTexCoordAndCornerIndex.xy + HalfPixel;
	out_vFrustumCornerVS = FrustumCornersVS[in_vTexCoordAndCornerIndex.z];
}

// Pixel shader for computing the shadow occlusion factor
float4 ShadowTermPS(	in float2 in_vTexCoord			: TEXCOORD0,
						in float3 in_vFrustumCornerVS	: TEXCOORD1,
						uniform int iFilterSize	)	: COLOR0
{
/*
	NOT USED
	float fSceneDepth = DecodeFloatRGBA(tex2D(DepthTextureSampler,in_vTexCoord)) * FAR_PLANE_DISTANCE;

	float4 vPositionVS = float4(normalize(in_vFrustumCornerVS) * fSceneDepth, 1);

	float diff = 0;
	float3 fShadowTerm1 = GetShadowTermFromPosition(vPositionVS, vPositionVS.z, iFilterSize, 0, diff);

	float blendDiff = vPositionVS.z / -10.0f;
	float testDepth = vPositionVS.z - blendDiff;

	float3 fShadowTerm2 = GetShadowTermFromPosition(vPositionVS, testDepth, iFilterSize, 0, diff);
	float blend = saturate(diff / blendDiff);

	return float4( fShadowTerm1 * (1 - blend) + fShadowTerm2 * blend, 1);
	*/
	return float4(0,0,0, 1);
}

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

VertexShaderInput ClearVS(VertexShaderInput input)
{
	//	We're using a full screen quad, no need for transforming vertices.
	VertexShaderInput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoord + HalfPixel;
	return output;
}

float4 ClearPS(VertexShaderInput input) : COLOR0
{
	return float4(1,1,1,1);
}

technique GenerateShadowMap
{
	pass p0
	{
		VertexShader = compile vs_2_0 GenerateShadowMapVS();
        PixelShader = compile ps_2_0 GenerateShadowMapPS();
	}
}

technique GenerateVoxelShadowMap
{
	pass p0
	{
		VertexShader = compile vs_2_0 GenerateVoxelShadowMapVS();
        PixelShader = compile ps_2_0 GenerateVoxelShadowMapPS();
	}
}

// Instancing requires SM 3.0
technique GenerateShadowMapInstanced
{
	pass p0
	{
		VertexShader = compile vs_3_0 GenerateShadowMapVS_Instanced();
        PixelShader = compile ps_3_0 GenerateShadowMapPS();
	}
}

technique GenerateShadowMapInstancedGeneric
{
	pass p0
	{
		VertexShader = compile vs_3_0 GenerateShadowMapVS_InstancedGeneric();
        PixelShader = compile ps_3_0 GenerateShadowMapPS();
	}
}


technique GenerateShadowMapSkinned
{
	pass p0
	{
		VertexShader = compile vs_3_0 GenerateShadowMapVS_Skinned();
        PixelShader = compile ps_3_0 GenerateShadowMapPS();
	}
}


technique CreateShadowTerm2x2PCF
{
    pass p0
    {
		ZWriteEnable = false;
		ZEnable = false;
		AlphaBlendEnable = false;
		CullMode = NONE;

        VertexShader = compile vs_3_0 ShadowTermVS();
        PixelShader = compile ps_3_0 ShadowTermPS(2);
    }
}

technique CreateShadowTerm3x3PCF
{
    pass p0
    {
		ZWriteEnable = false;
		ZEnable = false;
		AlphaBlendEnable = false;
		CullMode = NONE;

        VertexShader = compile vs_3_0 ShadowTermVS();
        PixelShader = compile ps_3_0 ShadowTermPS(3);
    }
}

technique CreateShadowTerm5x5PCF
{
    pass p0
    {
		ZWriteEnable = false;
		ZEnable = false;
		AlphaBlendEnable = false;
		CullMode = NONE;

        VertexShader = compile vs_3_0 ShadowTermVS();
        PixelShader = compile ps_3_0 ShadowTermPS(5);
    }
}

technique CreateShadowTerm7x7PCF
{
    pass p0
    {
		ZWriteEnable = false;
		ZEnable = false;
		AlphaBlendEnable = false;
		CullMode = NONE;

        VertexShader = compile vs_3_0 ShadowTermVS();
        PixelShader = compile ps_3_0 ShadowTermPS(7);
    }
}

technique Clear
{
	pass p0
	{
		VertexShader = compile vs_3_0 ClearVS();
        PixelShader = compile ps_3_0 ClearPS();
	}
}
