#ifndef TRIPLANAR_MATERIAL_CONSTANTS
#define TRIPLANAR_MATERIAL_CONSTANTS


struct TriplanarMaterialSlices
{
	int4 slices1; // x = cmXZnY, y = cmY, z = ngXZnY, w = ngY
	int2 slices2; // x = extXZnY, y = extY
	int2 _padding;
};
struct TriplanarMaterialConstants
{
	float4 distance_and_scale;  //x = initial scale, y = initial distance, z = scale multiplier, w = distance multiplier
	float3 distance_and_scale_far; //x = far1 texture scale, y = switch to far1 texture, z = slice index
	float _padding0;
	float3 distance_and_scale_far2;
	float _padding1;
	float3 distance_and_scale_far3;
	float extension_detail_scale;
	float4 color_far3;

	TriplanarMaterialSlices slices[3];
};

#endif