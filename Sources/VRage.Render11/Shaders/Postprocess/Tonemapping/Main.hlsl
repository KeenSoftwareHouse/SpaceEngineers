// @defineMandatory NUMTHREADS 8
// @define DISABLE_TONEMAPPING

#include "Filters.hlsli"
#include "Defines.hlsli"

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex)
{
	uint2 texel = dispatchThreadID.xy;
	float2 uv = (texel + 0.5f) / frame_.Screen.resolution;

    float3 sourceSample = Source[texel].xyz;
	float3 color = sourceSample;

#ifndef DISABLE_TONEMAPPING
    sourceSample += Bloom.SampleLevel(BilinearSampler, uv, 0).xyz * frame_.Post.BloomMult;
    float3 exposed_color = ExposedColor(sourceSample, 0);
    color = exposed_color;
#endif

#ifndef DISABLE_COLOR_FILTERS
    color = ApplyBasicFilters(color);
    // FIXME - does not work correctly
    //color = VibranceFilter(color);
    // FIXME - does not work correctly
    //color = TemperatureFilter(color);
    color = SepiaFilter(color);
#endif

	float alpha = 1;
#ifdef FXAA_ENABLED
	color = saturate(color);
	color = rgb_to_srgb(color);	
	alpha = calc_luminance(color);
#endif

	Destination[texel] = float4(color, alpha);
}
