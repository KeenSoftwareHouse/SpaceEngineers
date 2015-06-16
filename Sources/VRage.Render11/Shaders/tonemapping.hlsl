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

RWTexture2D<float4> Destination	: register( u0 );

SamplerState BilinearSampler : register( s0 );

#include <frame.h>

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

#include <math.h>

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

	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

float3 tonemap_operator(float3 color)  {
	return curve_tonemap(color);
}

float3 exposed_color(float3 color, float exposure)
{
    float avg_lum = max(get_avg_luminance(), 0.0001f);
    float linear_exposure = middle_grey() / avg_lum;
    linear_exposure = log2(max(linear_exposure, 0.0001f));
    linear_exposure += exposure;
    return exp2(linear_exposure) * color;
}

float3 calc_tonemap(float3 color)
{
    color = exposed_color(color, frame_.luminance_exposure);
    
    color = tonemap_operator(color);
    return color;
}

float3 calc_contrast(float3 color, float contrast) {
	float f = 259 * (contrast + 255) / (255 * (259 - contrast));
	return saturate(f * (color - 0.5) + 0.5);
}

//#define DISABLE_TONEMAPPING

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void tonemapping(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;
	float2 uv = (texel + 0.5f) / frame_.resolution;

	float3 sum = 0;
#if MSAA_ENABLED
	[unroll]
	for(int i=0; i<MS_SAMPLE_COUNT; i++) {
		float3 pixel = SourceMS.Load(texel, i).xyz;
#else
		float3 pixel = Source[texel].xyz;
#endif

#ifdef DISABLE_TONEMAPPING
	sum += pixel;
#else
	float3 color = calc_tonemap(pixel);
	color += tonemap_operator(Bloom.SampleLevel(BilinearSampler, uv, 0).xyz * frame_.bloom_mult);

	float L = calc_luminance(exposed_color(pixel, frame_.luminance_exposure));
	float sigma = eye_insensitivity(get_avg_luminance());
	const float3 blue_shift = float3(1.05f, 0.97f, 1.27f);

	sum += lerp(color, blue_shift * L, sigma);
#endif

#if MSAA_ENABLED
	}

	sum /= MS_SAMPLE_COUNT;
#endif

	sum = calc_contrast(sum + frame_.brightness, frame_.contrast * 255);
	float alpha = 1;
#ifdef FXAA_ENABLED
	sum = saturate(sum);
	sum = rgb_to_srgb(sum);	
	alpha = calc_luminance(sum);
#endif

	Destination[texel] = float4(sum, alpha);
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void bloom_initial(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;
	uint i;

	float4 red;
	float4 green;
	float4 blue;

	const uint2 offsets[4] = {
        uint2(0, 0), uint2(1, 0),
        uint2(1, 0), uint2(1, 1),
    };

    [unroll]
	for(int t=0; t<4; t++) {
		float3 tap = 0;
		uint2 tap_pos = texel * 2 + offsets[t];

#if !MSAA_ENABLED
		tap = Source[tap_pos].xyz;
#else 
		[unroll]
		for(i=0; i< MS_SAMPLE_COUNT; i++) {
			tap += SourceMS.Load(tap_pos, i).xyz;
		}
		tap /= (float) MS_SAMPLE_COUNT;	
#endif
		red[t] = tap.x;
		green[t] = tap.y;
		blue[t] = tap.z;
	}

	float4 result = 0;
	[unroll]
	for(i=0; i< 4; i++) {
		float3 color = float3(red[i], green[i], blue[i]);
		result.xyz += max(exposed_color(color, frame_.luminance_exposure + frame_.bloom_exposure), 0); // nans sometimes
	}
	
	result /= 4.f;

	Destination[texel] = result;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void downscale(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	const uint2 offsets[4] = {
        uint2(0, 0), uint2(1, 0),
        uint2(1, 0), uint2(1, 1),
    };

    // manual interpolation vs sending texcoords
    float4 result = 0;
    [unroll]
    for(uint i=0; i<4; i++) {
    	result += GenericSource[texel * 2 + offsets[i]];
    }
	Destination[texel] = result / 4.f;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void blur_v(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	float4 result = 0;
	for(int i=-6; i<6; i++) {
		float4 sample = GenericSource[texel + float2(i, 0)];

		result += sample * gaussian_weigth(i, 2);
	}

	Destination[texel] = result;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void blur_h(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	float4 result = 0;
	for(int i=-6; i<6; i++) {
		float4 sample = GenericSource[texel + float2(0, i)];

		result += sample * gaussian_weigth(i, 2);
	}

	Destination[texel] = result;
}