#include <postprocess_base.h>
#include <frame.h>

Texture2D	InputTexture0	: register( t0 );
#ifndef MS_SAMPLE_COUNT
#else
Texture2DMS<float4, MS_SAMPLE_COUNT> InputTexture0_Ms	: register( t0 );
#endif

float4 blur_h(PostprocessVertex input) : SV_Target0
{
	float4 result = 0;
	for(int i=-12; i<=12; i++) {
	
	#ifndef MS_SAMPLE_COUNT
		float4 sample = InputTexture0[input.position.xy + float2(i, 0)];
	#else
		float4 sample = InputTexture0_Ms.Load(input.position.xy + float2(i, 0), 0);
	#endif

		result += sample * gaussian_weigth(i, 2) * 2;
	}
	return float4(result.xyz, 0);
}

float4 blur_v(PostprocessVertex input) : SV_Target0
{
	float4 result = 0;
	for(int i=-12; i<=12; i++) {
		float4 sample = InputTexture0[input.position.xy + float2(0, i)];
		result += sample * gaussian_weigth(i, 2) * 2;
	}
	return float4(result.xyz, 0);
}