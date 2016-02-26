#ifndef __COLOR_H
#define __COLOR_H

// color space conversions

float3 hsv_to_rgb(float3 hsv)
{
    float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0f - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

float3 rgb_to_hsv(float3 rgb)
{
    float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 srgb_to_rgb(float3 srgb)
{
    float3 rgb = (srgb <= 0.04045) * srgb / 12.92;
    rgb += (srgb > 0.04045) * pow((abs(srgb) + 0.055) / 1.055, 2.4);
    return rgb;
}

float3 rgb_to_srgb(float3 rgb)
{
    return pow(rgb, 1 / 2.2f);
}

float calc_luminance(float3 rgb)
{
    //return dot(rgb, float3(0.2126, 0.7152, 0.0722));
    return dot(rgb, float3(0.299f, 0.587f, 0.114f));
}

#endif