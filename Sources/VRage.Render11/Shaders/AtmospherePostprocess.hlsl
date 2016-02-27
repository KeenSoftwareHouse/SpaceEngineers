#include <template.h>
#include <EnvAmbient.h>
#include <postprocess_base.h>

Texture2D<float>	DepthBuffer	: register(t0);


cbuffer Constants : register(MERGE(b, PROJECTION_SLOT))
{
	matrix viewMatrix;
};

Texture2D<float2> DensityLut : register(t2);

cbuffer AtmosphereConstants : register(b2) {
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

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0) {
	float depth = DepthBuffer[vertex.position.xy];
    clip(depth - 1);

	const float ray_x = 1;
	const float ray_y = 1;
	float3 screen_ray = float3(lerp(-ray_x, ray_x, vertex.uv.x), -lerp(-ray_y, ray_y, vertex.uv.y), -1.);

	float3 V = mul(screen_ray, transpose((float3x3)viewMatrix));

	output = ComputeAtmosphere(V, V * (RadiusAtmosphere - RadiusGround), depth, 0, 1)*0.2f;
	float max_component = max(max(max(output.r, output.g), output.b), 1.0f);
	output /= max_component;
}