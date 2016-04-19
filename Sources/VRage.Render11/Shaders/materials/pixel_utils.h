#include <Math/Color.h>
#include <Lighting/brdf.h>

float3x3 PixelTangentSpace(float3 N, float3 dp1, float3 dp2, float2 uv)
{
    float2 duv1 = ddx(uv);
    float2 duv2 = ddy(uv);
    float3 dp2perp = cross(dp2, N);
    float3 dp1perp = cross(N, dp1);

    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = rsqrt(max(dot(T, T), dot(B, B)));
    return float3x3(T * invmax, B * invmax, N);
}

float3x3 PixelTangentSpace(float3 N, float3 pos, float2 uv)
{
    return PixelTangentSpace(N, ddx(pos), ddy(pos), uv);
}

float3 NormalBuildTangent(float3 normalmap, float3 normal, float2 texcoord0, float3 pos_ws, out float normalLength)
{
    normalmap = normalmap * 2 - 1;
    normalmap.y *= -1;
    normalLength = length(normalmap);

    float3x3 tangent_to_world;
    float3 dPosX = ddx(pos_ws);
    float3 dPosY = ddy(pos_ws);

    tangent_to_world = PixelTangentSpace(normal, dPosX, dPosY, texcoord0);

    return normalize(mul(normalmap, tangent_to_world));
}

float3 Normal(float3 normalmap, float4 tangent, float3 normal, out float normalLength)
{
    normalmap = normalmap * 2 - 1;
    normalmap.y *= -1;
    normalLength = length(normalmap);

    float3 T = tangent.xyz;
    float3 B = cross(T, normal) * tangent.w;
    float3x3 tangent_to_world = float3x3(T, B, normal);

    return normalize(mul(normalmap, tangent_to_world));
}

float3 ColorizeGray(float3 texcolor, float3 hsvmask, float coloring)
{
    float3 coloringc = hsv_to_rgb(float3(hsvmask.x, 1, 1)); // TODO: probably won't optimize by itself

    // applying coloring & convert for masking
    float3 hsv = rgb_to_hsv(lerp(texcolor, texcolor * coloringc, coloring));

    hsv.x = 0;
    float3 fhsv = hsv + hsvmask * float3(1, 1, 0.5); // magic, matches colors from se better
    fhsv.x = frac(fhsv.x);

    float gray2 = 1 - saturate((hsvmask.y + 1.0f) / 0.1f);
    fhsv.yz = lerp(saturate(fhsv.yz), saturate(hsv.yz + hsvmask.yz), gray2);

    float gray3 = 1 - saturate((hsvmask.y + 0.9f) / 0.1f);
    fhsv.y = lerp(saturate(fhsv.y), saturate(hsv.y + hsvmask.y), gray3);

    return lerp(texcolor, hsv_to_rgb(fhsv), coloring);
}

float3 ColorizeAll(float3 texcolor, float3 hsvmask, float coloring)
{
    float3 c_rgb = hsv_to_rgb(saturate(hsvmask.xyz * float3(1, 0.5, 0.5) + float3(0, 0.5, 0.5)));
    return lerp(texcolor, texcolor * c_rgb, coloring);
}

#ifdef ALPHA_MASKED
int AlphamaskCoverageAndClip(float threshold, float2 texcoord0)
{
#if !defined(MS_SAMPLE_COUNT) || defined(DEPTH_ONLY)
    float alpha = AlphamaskTexture.Sample(TextureSampler, texcoord0).x;
    clip(alpha - threshold);
    return 0;
#else
    int coverage = 0;
    [unroll]
    for (int s = 0; s < MS_SAMPLE_COUNT; s++)
    {
        float2 sample_texcoord = EvaluateAttributeAtSample(texcoord0, s);
        float alpha = AlphamaskTexture.Sample(TextureSampler, sample_texcoord).x;
        coverage |= (alpha > threshold) ? (uint(1) << s) : 0;
    }
    clip(coverage - 1);
    return coverage;
#endif
}
#endif

#ifndef DEPTH_ONLY
void FeedOutputInternal(PixelInterface pixel, inout MaterialOutputInterface output, float4 cm, float4 extras)
{
    // color
    if (pixel.material_flags & MATERIAL_FLAG_NO_KEYCOLOR)
        output.base_color = cm.xyz;
    else
    {
        if (pixel.material_flags & MATERIAL_FLAG_COLORING_RGB)
            output.base_color = ColorizeAll(cm.xyz, pixel.key_color.xyz, extras.w);
        else output.base_color = ColorizeGray(cm.xyz, pixel.key_color.xyz, extras.w);
    }

    output.metalness = cm.w;
    output.base_color *= pixel.color_mul;
    output.transparency = 0;

    // bc7 compression artifacts can give byte value 1 for 0, which should more visible than small shift
    output.emissive = max(output.emissive, saturate(extras.y - 1 / 255. + pixel.emissive));

    output.ao = extras.x;

    // material
    output.id = pixel.material_index;
#if !defined(USE_VOXEL_DATA) && !defined(USE_MERGE_INSTANCING)
    if (object_.facing)
    {
        output.id = 2;
    }
#endif
}

void FeedOutputBuildTangent(PixelInterface pixel, float2 texcoord0, float3 normal, inout MaterialOutputInterface output, float4 ng, float4 cm, float4 extras)
{
    float normalLength;
    output.normal = NormalBuildTangent(ng.xyz, normal, texcoord0, pixel.position_ws, normalLength);

    output.gloss = ToksvigGloss(ng.w, min(normalLength, 1));

    FeedOutputInternal(pixel, output, cm, extras);
}

void FeedOutput(PixelInterface pixel, float4 tangent, float3 normal, inout MaterialOutputInterface output, float4 ng, float4 cm, float4 extras)
{
    float normalLength;
    output.normal = Normal(ng.xyz, tangent, normal, normalLength);

    output.gloss = ToksvigGloss(ng.w, min(normalLength, 1));

    FeedOutputInternal(pixel, output, cm, extras);
}
#endif

float3 Hologram(float3 screen_position, float custom_alpha)
{
    float tex_dither = Dither8x8[(uint2)screen_position.xy % 8];
    clip(tex_dither + custom_alpha);

    float t = frame_.time / 10.0;
    float2 screenPos = screen_to_uv(screen_position.xy) * 2 - 1;
    float2 param = float2(t, screenPos.x + screenPos.y);
    float flicker = frac(sin(dot(param, float2(12.9898, 78.233))) * 43758.5453) * 0.2 + 0.8;

    float offset = t * 500.0 * 0.2 + frac(sin(dot(screenPos.x, float2(12.9898, 78.233))) * 43758.5453) * 1.5;
    float3 overlay = Dither8x8.Sample(LinearSampler, frac((screenPos.yy * 8.0 + offset / 16.0) + float2(0, 0.8)));
    float3 holoColor = flicker * pow(abs(overlay), 1.5);

    if (custom_alpha >= -0.25)
        holoColor *= 1.5;
    return holoColor;
}

void Dither(float3 screen_position, float custom_alpha)
{
#ifdef DITHERED
    float tex_dither = Dither8x8[(uint2)screen_position.xy % 8];
#else
    float tex_dither = rand(screen_position.xy);
#endif
    float object_dither = abs(custom_alpha);

    if (object_dither > 1)
    {
        object_dither -= 1.0f;
        object_dither = 1.0f - object_dither;
        clip(object_dither - tex_dither);
    }
    else clip(tex_dither - object_dither);
}
