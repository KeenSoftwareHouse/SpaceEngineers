#include <postprocess_base.h>

// The opaque scene depth buffer read as a texture
Texture2D<float4>     g_AccumTexture                    : register(t0);

// The texture atlas for the particles
Texture2D<float2>     g_CoverageTexture                 : register(t1);

// Ratserization path's pixel shader
float4 __pixel_shader(PostprocessVertex input) : SV_Target0
{ 
    float4 accum = g_AccumTexture[int2(input.position.xy)];
    float2 reveal = g_CoverageTexture[int2(input.position.xy)];

    //  ONE, SRC_ALPHA
    float overdraw = 1 + saturate(abs((1 - reveal.r) - accum.a));
    //float overdraw = 1 + saturate(abs(reveal.r - accum.a));
    return float4(accum.rgb / max(accum.a, 1e-5) * overdraw, reveal.r);
}
