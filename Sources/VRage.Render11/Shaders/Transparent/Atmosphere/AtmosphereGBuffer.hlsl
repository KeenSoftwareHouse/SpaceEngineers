// @define SAMPLE_FREQ_PASS

#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#include "AtmosphereCommon.hlsli"

#include <Frame.hlsli>
#include <GBuffer/GBuffer.hlsli>

// additive blend
void __pixel_shader(float4 svPos : SV_Position, out float4 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	) 
{
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(svPos.xy);
#else
	SurfaceInterface input = read_gbuffer(svPos.xy, sample_index);
#endif

	// We need to clamp it to mitigate artefacts in the tonemapper
	// 10 is the smallest value for which it still looks acceptable
    //output = float4(input.position, 0);
	output = ComputeAtmosphere(input.V, input.position, frame_.Light.directionalLightVec, input.depth, input.native_depth, 4);
}
