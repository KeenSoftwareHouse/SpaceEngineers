struct MaterialConstants
{
	float4 distance_and_scale[3]; // same as in single
	float4 distance_and_scale_far[3];
	float2 distance_and_scale_far3[3];
	float4 color3_far[3];
	float3 extension_detail_scale;
	float padding;
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
	float3 texcoords	: TEXCOORD0;
	float3 mat_weights	: TEXCOORD1;
	float distance		: TEXCOORD2;
	float3x3 world_matrix : TEXCOORD3;
	float ambient_occlusion : Ambient0;
};

Texture2DArray<float4> ColorMetal_BottomSides_Up[9] : register(t0);
Texture2DArray<float4> NormalGloss_BottomSides_Up[9] : register(t9);
Texture2DArray<float4> Ext_BottomSides_Up[9] : register(t18);
