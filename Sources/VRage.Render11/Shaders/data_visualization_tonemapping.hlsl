#include <data_visualization.h>

float3 curve_tonemap(float3 x)
{
	const float A = frame_.tonemapping_A;
	const float B = frame_.tonemapping_B;
	const float C = frame_.tonemapping_C;
	const float D = frame_.tonemapping_D;
	const float E = frame_.tonemapping_E;
	const float F = frame_.tonemapping_F;

	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void __pixel_shader(PostprocessVertex input, out float4 output : SV_Target0) {
	float x = float(input.uv.x * 5);
	float y = (1 - input.uv.y);
	float f_x = curve_tonemap(x.xxx).x;
	output = (y < f_x) * float4(1,1,1,0) * 0.25f;
	output += (abs(y - x) < 0.01f) * float4(0,1,0,0);
}