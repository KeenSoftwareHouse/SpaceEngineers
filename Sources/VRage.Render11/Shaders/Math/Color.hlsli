#ifndef COLOR_H
#define COLOR_H

// Some funcions taken from: http://www.chilliant.com/rgb2hsv.html

float COLOR_EPSILON = 1e-10;

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

float4 srgba_to_rgba(float4 srgb)
{
    float4 rgb = (srgb <= 0.04045) * srgb / 12.92;
    rgb += (srgb > 0.04045) * pow((abs(srgb) + 0.055) / 1.055, 2.4);
    return rgb;
}

float3 rgb_to_srgb(float3 rgb)
{
    return pow(rgb, 1 / 2.2f);
}

float linearizeColor(float component)
{
    float lC = (component <= 0.04045) * component / 12.92;
    lC += (component > 0.04045) * pow((abs(component) + 0.055) / 1.055, 2.4);
    return lC;
}

float calc_luminance(float3 rgb)
{
	//return dot(rgb, float3(0.212671, 0.715160, 0.072169) );		// Defined by sRGB gamut
    return dot(rgb, float3(0.299f, 0.587f, 0.114f));
}

// Returns relative luminance. To not be confused with "luma" which
// is defined by same coefficients multiplied by gamma compressed
// sRGB values.
float GetRelativeLuminance(float3 rgb)
{
    // ITU-R BT.709 Coefficients
    // http://www.itu.int/rec/R-REC-BT.709
    return dot(rgb, float3(0.2126, 0.7152, 0.0722));
}

float3 HUEToRGB(float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R, G, B));
}

float3 RGBToHCV(float3 rgb)
{
    // Based on work by Sam Hocevar and Emil Persson
    float4 P = (rgb.g < rgb.b) ? float4(rgb.bg, -1.0, 2.0 / 3.0) : float4(rgb.gb, 0.0, -1.0 / 3.0);
    float4 Q = (rgb.r < P.x) ? float4(P.xyw, rgb.r) : float4(rgb.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + COLOR_EPSILON) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBToHSL(float3 rgb)
{
    float3 hcv = RGBToHCV(rgb);
    float L = hcv.z - hcv.y * 0.5;
    float S = hcv.y / (1 - abs(L * 2 - 1) + COLOR_EPSILON);
    return float3(hcv.x, S, L);
}

float3 HSLToRGB(float3 hsl)
{
    float3 rgb = HUEToRGB(hsl.x);
    float C = (1 - abs(2 * hsl.z - 1)) * hsl.y;
    return (rgb - 0.5) * C + hsl.z;
}

float MiddleGrey(float avgLuminance) 
{
    // "Fast Filtering and Tone Mapping using Importance sampling",
    // Balázs Tóth and László Szirmay-Kalos
	return 1.03f - 2/(2 + log10(avgLuminance + 1));
}

float CalculateExposure(float avgLuminance, float exposure)
{
    float avg_lum = max(avgLuminance, 0.0001f);
    float linear_exposure = MiddleGrey(avgLuminance) / avg_lum;
    linear_exposure = log2(max(linear_exposure, 0.0001f));
    linear_exposure += exposure;
	return linear_exposure;
}

#endif // COLOR_H
