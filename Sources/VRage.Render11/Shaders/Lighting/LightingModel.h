#ifndef LIGHTING_MODEL_H__
#define LIGHTING_MODEL_H__

#include <Frame.h>
#include <Surface.h>
#include <Lighting/brdf.h>

float falloff_to_radius(float f, float range) 
{
	float r_ = 0.333933902326 * sqrt(6.12430005749 + 5.9892092 * f) - 0.814156676978;
	return range * r_;
}

float3 calculate_light(SurfaceInterface surface, float3 lightVector, float3 lightColor, float sFactor)
{
	float3 N = surface.N;
	float3 V = surface.V;
	float3 H = normalize(V + lightVector);

    float3 lightRadiance = lightColor * MaterialRadiance(surface.albedo, surface.f0, surface.gloss * sFactor, N, lightVector, V, H, surface.id);
	return lightRadiance;
	//return (1 - surface.emissive) * M_PI * lightRadiance;
}

float3 main_directional_light(SurfaceInterface surface)
{
    return calculate_light(surface, -frame_.directionalLightVec, frame_.directionalLightColor, 0.5f);
}

float3 back_directional_light(SurfaceInterface surface, int index)
{
    float3 lightVector = -normalize(frame_.additionalSunDirection[index].xyz);
	return calculate_light(surface, lightVector, frame_.additionalSunIntensity * frame_.additionalSunColor, 0.5f);
}

// scattering

float uniform_fog(float dist, float density) 
{
	return 1 - exp(-dist * density);
}

float3 add_fog(float3 color, float dist, float3 ray, float3 viewer)
{
	dist = clamp(dist, 0, 1000);
	float c = frame_.fog_mult;
	float b = frame_.fog_density;

	float3 fog_color = D3DX_R8G8B8A8_UNORM_to_FLOAT4(frame_.fog_color).xyz;
	float fog0 = c * uniform_fog(dist, b);

    return lerp( color, fog_color, fog0 );
}

#endif