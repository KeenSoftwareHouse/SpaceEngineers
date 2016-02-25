#include <blur_planet.h>

float4 __pixel_shader(PostprocessVertex input) : SV_Target0
{
	return float4(blur(input.position.xy, float2(0, 1)), 0);

}