#include "../TriplanarMaterialConstants.hlsli"

struct MaterialConstants
{
	TriplanarMaterialConstants triplanarMaterials[3];
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
	float3 texcoords	: TEXCOORD0;
	float3 mat_weights	: TEXCOORD1;
	float distance		: TEXCOORD2;
    float3x3 world_matrix : TEXCOORD3;
	float colorBrightnessFactor : Ambient0;
};

Texture2DArray<float4> ColorMetal : register(t0);
Texture2DArray<float4> NormalGloss : register(t1);
Texture2DArray<float4> Ext : register(t2);
