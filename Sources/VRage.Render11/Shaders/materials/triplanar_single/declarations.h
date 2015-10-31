struct MaterialConstants
{
	float4 distance_and_scale;  //x = initial scale, y = initial distance, z = scale multiplier, w = distance multiplier
	float4 distance_and_scale_far; //x = far1 texture scale, y = switch to far1 texture, z = far2 texture scale, w = switch to far2 texture
	float2 distance_and_scale_far3;
	float extension_detail_scale;
	float _padding;
	float4 color_far3;	
};

struct MaterialVertexPayload
{
	float3 normal 		: NORMAL;
//#ifndef DEPTH_ONLY
	float3 texcoords	: TEXCOORD0;
	float distance	: TEXCOORD1;
	float3x3 world_matrix : TEXCOORD2;
	float ambient_occlusion : Ambient0;
//#endif
};

Texture2DArray<float4> ColorMetal_BottomSides_Up[3] : register(t0);
Texture2DArray<float4> NormalGloss_BottomSides_Up[3] : register(t3);
Texture2DArray<float4> Ext_BottomSides_Up[3] : register(t6);
