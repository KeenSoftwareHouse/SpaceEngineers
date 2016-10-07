#include <Postprocess/PostprocessBase.hlsli>

// The opaque scene depth buffer read as a texture
Texture2D<float4>     g_AccumTexture                    : register(t0);

// The texture atlas for the particles
Texture2D<float2>     g_CoverageTexture                 : register(t1);

// Ratserization path's pixel shader
float4 __pixel_shader(PostprocessVertex input) : SV_Target0
{ 
    float4 accum = g_AccumTexture[int2(input.position.xy)];
    float2 reveal = g_CoverageTexture[int2(input.position.xy)];

    float3 averageColor = accum.rgb / max(accum.a, 1e-5);
    float alpha = 1 - reveal.r;

    //  ONE, SRC_ALPHA
    return float4(saturate(averageColor * alpha), reveal.r);
}
