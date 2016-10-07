#pragma once

#include "..\Common.hlsli"
#include "..\Frame.hlsli"
#include "..\VertexTransformations.hlsli"


static const uint MAX_CASCADES_COUNT = 8;
Texture2D<float> Depth : register(t0);
Texture2D<uint2> Stencil : register(t1);
Texture2DArray<float> CSM : register(t2);
Texture2D<float4> GBuffer1 : register(t3);
SamplerComparisonState	LinearComparisonSampler	: register(s6);


struct CascadeItem
{
	matrix worldToShadowSpace;
	float shadowNormalOffset;
	float3 padding;
};

cbuffer cb : register(b1)
{
	CascadeItem cascades[8];
}


uint GetCascadeIdFromStencil(uint stencil)
{
	return 0xf - (0xf & stencil);
}

float3 GetWorldToShadowmapSpace(float3 worldPosition, matrix shadowMatrix)
{
	float4 shadowmapPosition = mul(float4(worldPosition, 1), shadowMatrix);
		return shadowmapPosition.xyz / shadowmapPosition.w;
}

float3 GetShadowNormalOffset(float2 screencoord, int cascadeIndex)
{
	float offsetScalar = cascades[cascadeIndex].shadowNormalOffset;
	//if (cascadeIndex == 1)
	//	offsetScalar = 100;

	float4 gbuffer1 = GBuffer1[screencoord];
	float3 nview = unpack_normals2(gbuffer1.xy);
	float3 normal = view_to_world(nview);
	return normal * offsetScalar;
}

float GetHardShadow(matrix mWorldToShadowSpace, float3 worldPos, int cascadeIndex)
{
	float3 shadowmapPos = GetWorldToShadowmapSpace(worldPos, mWorldToShadowSpace);
	float depthInGbuffer = shadowmapPos.z;

	float3 shadowmapCoord = float3(shadowmapPos.xy, cascadeIndex);
	float depthInShadowmap = CSM.Sample(PointSampler, shadowmapCoord).x;

	return depthInShadowmap < depthInGbuffer ? 0 : 1.0f;
}

float GetSimpleShadow(matrix mWorldToShadowSpace, float3 worldPos, int cascadeIndex)
{
	float3 shadowmapPos = GetWorldToShadowmapSpace(worldPos, mWorldToShadowSpace);
	float depthInGbuffer = shadowmapPos.z;

	float3 shadowmapCoord = float3(shadowmapPos.xy, cascadeIndex);
	return CSM.SampleCmpLevelZero(LinearComparisonSampler, shadowmapCoord, depthInGbuffer);
}
