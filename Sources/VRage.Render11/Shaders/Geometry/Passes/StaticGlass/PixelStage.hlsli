#include "Declarations.hlsli"

#include <Lighting/EnvAmbient.hlsli>

#define OIT
#include <Transparent/OIT/Globals.hlsli>

struct GlassConstants
{
    float4 Color;
    float Reflective;
    float __p0, __p1, __p2;
};

Texture2D<float4> Texture : register(t0);

cbuffer GlassConstants : register (b2)
{
    GlassConstants Constants;
};

void __pixel_shader(VertexStageOutput vertex, out float4 accumTarget : SV_TARGET0, out float4 coverageTarget : SV_TARGET1)
{
#if defined(DITHERED_LOD) || defined(DITHERED)
    if (vertex.custom_alpha >= 0)
        Dither(vertex.position.xyz, vertex.custom_alpha);
#endif
    float4 textureSample = Texture.Sample(LinearSampler, vertex.texcoord.xy);
    float4 color = Constants.Color;
    float reflective = Constants.Reflective;

    color.xyz *= color.w;

    float3 viewVector = normalize(get_camera_position() - vertex.positionw);
    float3 reflectionSample = ambient_specular(0.04f, 0.95f, vertex.normal, viewVector);
    float3 reflectionColor = lerp(color.xyz*color.w, reflectionSample, Constants.Reflective);
    float3 colorAndDirt = lerp(reflectionColor, textureSample.xyz, textureSample.w);
    float4 resultColor = float4(colorAndDirt, max(max(color.w, reflective), textureSample.w));

    // hotfix for holograms:
    if (vertex.custom_alpha < 0)
    {
        resultColor.xyz *= Hologram(vertex.position.xyz, vertex.custom_alpha);
        //output.emissive = 1;
    }

    float linearDepth = linearize_depth(vertex.position.z, frame_.Environment.projection_matrix);
    TransparentColorOutput(resultColor, linearDepth, vertex.position.z, 1.0f, accumTarget, coverageTarget);
}
