// @defineMandatory NUMTHREADS 8
// @define DISABLE_TONEMAPPING

#include <tonemapping.h>

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
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
		float3 sourceSample = SourceMS.Load(texel, i).xyz;
#else
		float3 sourceSample = Source[texel].xyz;
#endif

#ifdef DISABLE_TONEMAPPING
	sum += sourceSample;
#else
    float3 color = TonemappedColor(sourceSample);
    color += TonemapOperator(Bloom.SampleLevel(BilinearSampler, uv, 0).xyz * frame_.bloom_mult);

    float L = calc_luminance(exposed_color(sourceSample, frame_.luminance_exposure));
	float sigma = eye_insensitivity(get_avg_luminance());
	const float3 blue_shift = float3(1.05f, 0.97f, 1.27f);

	sum += lerp(color, blue_shift * L, sigma);
#endif

#if MSAA_ENABLED
	}

	sum /= MS_SAMPLE_COUNT;
#endif

	sum = CalculateContrast(sum + frame_.brightness, frame_.contrast * 255);
	float alpha = 1;
#ifdef FXAA_ENABLED
	sum = saturate(sum);
	sum = rgb_to_srgb(sum);	
	alpha = calc_luminance(sum);
#endif

	Destination[texel] = float4(sum, alpha);
}
