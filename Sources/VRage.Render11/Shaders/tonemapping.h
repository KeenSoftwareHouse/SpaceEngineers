#include <frame.h>
#include <Math/Color.h>

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

#ifdef MS_SAMPLE_COUNT
#define MSAA_ENABLED 1
#else
#define MSAA_ENABLED 0
#endif

#if !MSAA_ENABLED
Texture2D<float4> Source : register( t0 );
#else 
Texture2DMS<float4, MS_SAMPLE_COUNT> SourceMS : register( t0 );
#endif

Texture2D<float> AvgLuminance : register( t1 );
Texture2D<float4> Bloom : register( t2 );

Texture2D<float4> GenericSource : register( t0 );

RWTexture2D<float4> Destination : register( u0 );

SamplerState BilinearSampler : register( s0 );


cbuffer Constants : register( b1 )
{
	float Middle_grey;
	float Exposure_offset;
	float Bloom_exposure;
	float Bloom_mult;
};

float get_avg_luminance() {
	return AvgLuminance[uint2(0,0)].r;
}

float middle_grey() {
	if(frame_.middle_grey > 0) 
		return frame_.middle_grey;

	// paper curve
	//return 1.03f - 2/(2 + log10(get_avg_luminance() + 1));

	// my curve
	float A = frame_.middle_grey_curve_sharpness; // sharpness
	float C = frame_.middle_grey_0; // at 0
	const float B = C + A * 0.5;
	return B - A / (2 + log10(get_avg_luminance() + 1));
}

float eye_insensitivity(float Y) {
	// paper curve
	//return 0.04f / ( 0.04f + Y );

	// my curve
	float A = frame_.blue_shift_rapidness; // sharpness
	float B = frame_.blue_shift_scale; // scale
	return B * A / (A + Y);
}

float3 tonemap_filmic(float3 color) {
    color = max(0, color - 0.004f);
    color = (color * (6.2f * color + 0.5f)) / (color * (6.2f * color + 1.7f)+ 0.06f);
    return pow(color, 2.2f);
}

float3 tonemap_reinhard(float3 color) {
	float lum = calc_luminance(color);
    return color * lum / (1 + lum);
}

float3 curve_tonemap(float3 x)
{
	const float A = frame_.tonemapping_A;
	const float B = frame_.tonemapping_B;
	const float C = frame_.tonemapping_C;
	const float D = frame_.tonemapping_D;
	const float E = frame_.tonemapping_E;
	const float F = frame_.tonemapping_F;

    return mad(mad(A, x, C*B), x, D*E)/mad(mad(A, x, B), x, D*F) - E/F;
}

#define TonemapOperator curve_tonemap

float3 exposed_color(float3 color, float exposure)
{
    float avg_lum = max(get_avg_luminance(), 0.0001f);
    float linear_exposure = middle_grey() / avg_lum;
    linear_exposure = log2(max(linear_exposure, 0.0001f));
    linear_exposure += exposure;
    return exp2(linear_exposure) * color;
}

float3 TonemappedColor(float3 color)
{
    color = exposed_color(color, frame_.luminance_exposure);
    
    color = TonemapOperator(color);
    return color;
}

float3 CalculateContrast(float3 color, float contrast) {
	float f = 259 * (contrast + 255) / (255 * (259 - contrast));
	return saturate(f * (color - 0.5) + 0.5);
}
