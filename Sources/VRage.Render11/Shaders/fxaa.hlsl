#include <common.h>
#include <postprocess_base.h>
#include <frame.h>

#define FXAA_PC 1
#define FXAA_HLSL_5 1
#define FXAA_QUALITY__PRESET 29 // 15/29

#include <Fxaa3_11.h>

Texture2D	Source	: register( t0 );

float4 fxaa(PostprocessVertex input) : SV_Target0
{
	FxaaTex tex = { LinearSampler, Source };

	float3 result = FxaaPixelShader( input.uv, 0, tex, tex, tex, 
		1/frame_.resolution, 0, 0, 0, 
		1, // try 1, sth about subpixel
		0.125,
		0.0625,
		0, 0, 0, 0
		).xyz;	

	return float4(result, 1);
}
