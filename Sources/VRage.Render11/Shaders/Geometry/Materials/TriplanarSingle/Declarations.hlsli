#include <Common.hlsli>
#include "../TriplanarMaterialConstants.hlsli"

struct MaterialConstants
{
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
	float3 texcoords	: TEXCOORD0;
	float distance	: TEXCOORD1;

	float mat_idx : TEXCOORD2;

    float3x3 world_matrix : TEXCOORD3;
    float colorBrightnessFactor : Ambient0;
};

Texture2DArray<float4> ColorMetal : register(t0);
Texture2DArray<float4> NormalGloss : register(t1);
Texture2DArray<float4> Ext : register(t2);
