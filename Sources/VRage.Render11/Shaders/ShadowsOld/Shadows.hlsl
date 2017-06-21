#include <Frame.hlsli>
#include <Math/Math.hlsli>
#include "Csm.hlsli"

static const float2 PoissonSamplesArray[] = {
	float2(0.130697, -0.209628),
	float2(-0.112312, 0.327448),
	float2(-0.499089, -0.030236),
	float2(0.332994, 0.380106),
	float2(-0.234209, -0.557516),
	float2(0.695785, 0.066096),
	float2(-0.419485, 0.632050),
	float2(0.678688, -0.447710),
	float2(0.333877, 0.807633),
	float2(-0.834613, 0.383171),
	float2(-0.682884, -0.637443),
	float2(0.769794, 0.568801),
	float2(-0.087941, -0.955035),
	float2(-0.947188, -0.166568),
	float2(0.425303, -0.874130),
	float2(-0.134360, 0.982611),
};

static const float2 RandomRotation[] = {
	float2(0.971327, 0.237749),
	float2(-0.885968, 0.463746),
	float2(-0.913331, 0.407218),
	float2(0.159352, 0.987222),
	float2(-0.640909, 0.767617),
	float2(-0.625570, 0.780168),
	float2(-0.930406, 0.366530),
	float2(-0.940038, 0.341070),
	float2(0.964899, 0.262621),
	float2(-0.647723, 0.761876),
	float2(0.663773, 0.747934),
	float2(0.929892, 0.367833),
	float2(-0.686272, 0.727345),
	float2(-0.999057, 0.043413),
	float2(-0.710684, 0.703511),
	float2(-0.893640, 0.448784)
};

static const uint PoissonSamplesNum = 12;
static const float FilterSize = 3;

Texture2D<float>	DepthBuffer	: register(t0);

#ifndef MS_SAMPLE_COUNT
Texture2D<uint2>	Stencil		: register(t1);
#else
Texture2DMS<uint2, MS_SAMPLE_COUNT>	StencilMS	: register(t1);
#endif

RWTexture2D<float> Output : register(u0);

float GetShadowmapValue(int2 Texel, uint2 rotationOffset)
{
	float2 screenUV = (Texel + 0.5f - frame_.Screen.offset_in_gbuffer) / frame_.Screen.resolution;
		float3 worldPosition = ReconstructWorldPosition(DepthBuffer[Texel], screenUV);

#ifndef MS_SAMPLE_COUNT
	int stencilValue = Stencil[Texel].y;
	if (! (stencilValue & 0x0f)) // check whether the pixel should be discarded
		return 1;
	uint cascadeIndex = cascade_id_stencil(stencilValue);
#else
	int stencilValue = StencilMS.Load(Texel, 0).y;
	if (! (stencilValue & 0x0f)) // check whether the pixel should be discarded
		return 1;
	uint cascadeIndex = cascade_id_stencil(StencilMS.Load(Texel, 0).y);
#endif

	float3 shadowmapPosition = WorldToShadowmap(worldPosition, csm_.cascade_matrix[cascadeIndex]);

	float texelSize = rcp(csm_.resolution);
	texelSize *= cascadeIndex > 0 ? 2.0f : 1.0f;
	float2 filterSize = csm_.cascade_scale[cascadeIndex].xy * FilterSize;
	float2 filterTexelSize = texelSize*filterSize;

#ifdef ENABLE_DISTORTION
	float2 theta = RandomRotation[rotationOffset.y * 4 + rotationOffset.x].xy;
	float2x2 rotMatrix = float2x2(float2(theta.xy), float2(-theta.y, theta.x));
#endif

#ifdef ENABLE_PCF
	float result = 0;
	[unroll]
	for (uint sampleIndex = 0; sampleIndex < PoissonSamplesNum; ++sampleIndex)
	{
		float2 offset = filterTexelSize * PoissonSamplesArray[sampleIndex];
#ifdef ENABLE_DISTORTION
		offset = mul(rotMatrix, offset);
#endif
		result += CSM.SampleCmpLevelZero(ShadowmapSampler, float3(shadowmapPosition.xy + offset, cascadeIndex), shadowmapPosition.z);
	}

	return result / PoissonSamplesNum;
#else
	return CSM.SampleCmpLevelZero(ShadowmapSampler, float3(shadowmapPosition.xy, cascadeIndex), shadowmapPosition.z);
#endif
}

[numthreads(32, 32, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	float2 texel = frame_.Screen.offset_in_gbuffer + dispatchThreadID.xy;
	Output[texel] = GetShadowmapValue(texel, dispatchThreadID.xy % 4);
}