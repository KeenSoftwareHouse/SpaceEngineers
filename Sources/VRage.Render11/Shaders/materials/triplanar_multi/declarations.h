struct MaterialConstants
{
	float4 scales[3]; // same as in single
	float4 far_scales[3];
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
	float3 texcoords	: TEXCOORD0;
	float3 mat_weights	: TEXCOORD1;
	float distance		: TEXCOORD2;
};

Texture2DArray<float4> ColorMetal_BottomSides_Up[9] : register(t0);
Texture2DArray<float4> NormalGloss_BottomSides_Up[9] : register(t9);
Texture2DArray<float4> Ext_BottomSides_Up[9] : register(t18);