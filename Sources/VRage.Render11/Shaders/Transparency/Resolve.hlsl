#include <postprocess_base.h>

// The opaque scene depth buffer read as a texture
Texture2D<float4>     g_AccumTexture                    : register(t0);

// The texture atlas for the particles
Texture2D<float>      g_CoverageTexture                 : register(t1);

// Ratserization path's pixel shader
float4 __pixel_shader(PostprocessVertex input) : SV_TARGET
{ 
	float4 accum = g_AccumTexture[int2(input.position.xy)];
	float reveal = g_CoverageTexture[int2(input.position.xy)];

	// Blend Func: GL_ONE_MINUS_SRC_ALPHA, GL_SRC_ALPHA
	return float4(accum.rgb / max(accum.a, 0.0001f), reveal);
}
