#include <Frame.hlsli>
#include <Math/Color.hlsli>
#include <Math/Math.hlsli>
#include <Postprocess/Defines.hlsli>

float get_avg_luminance() 
{
    return AvgLuminance[uint2(0,0)].r;
}

float get_exposure() 
{
	return AvgLuminance[uint2(0,0)].g;
}

float3 ExposedColor(float3 color, float exposure)
{
    return exp2(get_exposure() + exposure) * color;
}

float3 GrayScaleFilter(float3 color)
{
    float3 grayXfer = float3(0.3, 0.59, 0.11);
    return dot(grayXfer, color);
}

float3 SepiaFilter(float3 color)
{
    float3 gray = GrayScaleFilter(color);
    float3 sepia = lerp(frame_.Post.DarkColor, frame_.Post.LightColor, gray);
    return lerp(color.rgb, sepia, frame_.Post.SepiaStrength);
}

float4x4 GetSaturationMatrix()
{
    // saturation
    // weights to convert linear RGB values to luminance
    const float rwgt = 0.3086;
    const float gwgt = 0.6094;
    const float bwgt = 0.0820;
    float s = frame_.Post.Saturation;
    return float4x4(
        (1 - s)*rwgt + s,   (1 - s)*rwgt,       (1 - s)*rwgt,       0,
        (1 - s)*gwgt,       (1 - s)*gwgt + s,   (1 - s)*gwgt,       0,
        (1 - s)*bwgt,       (1 - s)*bwgt,       (1 - s)*bwgt + s,   0,
        0,                  0,                  0,                  1);
}

// Apply brightness, contrast, saturation
// Kindly provided by Jan Hlousek, Centauri Production
float3 ApplyBasicFilters(float3 color)
{
    // construct color matrix
    // brightness - scale around (0.0, 0.0, 0.0)
    float brightnessR = frame_.Post.Brightness * frame_.Post.BrightnessFactorR;
    float brightnessG = frame_.Post.Brightness * frame_.Post.BrightnessFactorG;
    float brightnessB = frame_.Post.Brightness * frame_.Post.BrightnessFactorB;
    float4x4 brightnessMatrix = CreateScaleMatrix(brightnessR, brightnessG, brightnessB);

    // contrast - scale around (0.5, 0.5, 0.5)
    float4x4 contrastMatrix = CreateTranslationMatrix(-0.5);
    contrastMatrix = mul(contrastMatrix, CreateScaleMatrix(frame_.Post.Contrast));
    contrastMatrix = mul(contrastMatrix, CreateTranslationMatrix(0.5));

    float4x4 saturationMatrix = GetSaturationMatrix();

    // composite together matrices
    float4x4 m;
    m = brightnessMatrix;
    m = mul(m, contrastMatrix);
    m = mul(m, saturationMatrix);

    // this compiles to 3 dot products:
    return mul(float4(color, 1), m).rgb;
}

// Reference: http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
// See ColorExtensions.TemperatureToRGB() for Kelvins to RGB conversion algorithm
float3 TemperatureFilter(float3 color)
{
    //float3 filtered = lerp(color, frame_.Post.TemperatureColor, frame_.Post.TemperatureStrength);

    // Restore original luminance
    float3 originalHSL = RGBToHSL(color);
    float3 filteredHSL = RGBToHSL(frame_.Post.TemperatureColor);
    filteredHSL.xy = lerp(originalHSL.xy, filteredHSL.xy, frame_.Post.TemperatureStrength);
    return HSLToRGB(float3(filteredHSL.x, filteredHSL.y, originalHSL.z));
}

// Reference: https://delightlylinux.wordpress.com/2014/02/18/sweetfx-vibrance/
float3 VibranceFilter(float3 rgb)
{
    float luminance = GetRelativeLuminance(rgb);

    float minc = min(min(rgb.r, rgb.g), rgb.b);
    float maxc = max(max(rgb.r, rgb.g), rgb.b);

    float saturation = maxc - minc;

    float vibrance = frame_.Post.Vibrance;
    float s = 1.0 + (vibrance * (1.0 - (sign(vibrance) * saturation)));
    return lerp(luminance, rgb, s);
}
