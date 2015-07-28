struct MaterialConstants
{
	float4 scales;
    float2 far_scales;
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
//#ifndef DEPTH_ONLY
	float3 texcoords	: TEXCOORD0;
	float distance	: TEXCOORD1;

//#endif
};

Texture2DArray<float4> ColorMetal_BottomSides_Up[3] : register(t0);
Texture2DArray<float4> NormalGloss_BottomSides_Up[3] : register(t3);
Texture2DArray<float4> Ext_BottomSides_Up[3] : register(t9);
