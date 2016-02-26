// @define SAMPLE_FREQ_PASS

#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#include <frame.h>
#include <postprocess_base.h>
#include <gbuffer.h>
#include <vertex_transformations.h>

Texture2D<float2> DensityLut : register( t5 );

cbuffer AtmosphereConstants : register( b1 ) {
    matrix  WorldViewProj;
    float3  PlanetCenter;
    float   RadiusAtmosphere;
    float3  BetaRayleighScattering;
    float   RadiusGround;
    float3  BetaMieScattering;
	float   MieG;
    float2  HeightScaleRayleighMie;
	float	PlanetScaleFactor;
	float   AtmosphereScaleFactor;
	float   Intensity;
	float   FogIntensity;
	float2  __padding;
};

#include <AtmosphereCommon.h>

struct ProxyVertex
{
    float4 position : POSITION;
};

void __vertex_shader(float4 vertexPos : POSITION, out float4 svPos : SV_Position)
{
    svPos = mul(unpack_position_and_scale(vertexPos), WorldViewProj);
}

// additive blend
void __pixel_shader(float4 svPos : SV_Position, out float4 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	) {
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(svPos.xy);
#else
	SurfaceInterface input = read_gbuffer(svPos.xy, sample_index);
#endif
	// We need to clamp it to mitigate artefacts in the tonemapper
	// 10 is the smallest value for which it still looks acceptable
	output = clamp(0, ComputeAtmosphere(input.V, input.position, input.depth, input.native_depth, 10), 10);
}