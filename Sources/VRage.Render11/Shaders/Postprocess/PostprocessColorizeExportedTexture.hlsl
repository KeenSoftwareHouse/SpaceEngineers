#include "Postprocess.hlsli"
#include <Common.hlsli>
#include <Math/Color.hlsli>

cbuffer ColorBuffer : register(b1)
{
	float4 ColorMaskHSV;
};

float3 colorize_1(float3 texcolor, float3 hsvmask, float coloring)
{
	if (hsvmask.x == 0 && hsvmask.y == -1 && hsvmask.z == -1)
		return texcolor;

	float3 coloringc = hsv_to_rgb(float3(hsvmask.x, 1, 1)); // TODO: probably won't optimize by itself

		// applying coloring & convert for masking
		float3 hsv = rgb_to_hsv(lerp(1, coloringc, coloring) * texcolor);

		hsv.x = 0;
	float3 fhsv = hsv + hsvmask * float3(1, 1, 0.5); // magic, matches colors from se better
		fhsv.x = frac(fhsv.x);

	float gray2 = 1 - saturate((hsvmask.y + 1.0f) / 0.1f);
	fhsv.yz = lerp(saturate(fhsv.yz), saturate(hsv.yz + hsvmask.yz), gray2);

	float gray3 = 1 - saturate((hsvmask.y + 0.9f) / 0.1f);
	fhsv.y = lerp(saturate(fhsv.y), saturate(hsv.y + hsvmask.y), gray3);

	return lerp(texcolor, hsv_to_rgb(fhsv), coloring);
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0)
{
	float4 texcolor;
#ifndef MS_SAMPLE_COUNT
	texcolor = Source[input.position.xy];
#else
	texcolor = Source.Load(input.position.xy, 0);
#endif

	output = float4(colorize_1(texcolor.rgb, ColorMaskHSV.xyz, 1), 1);
}