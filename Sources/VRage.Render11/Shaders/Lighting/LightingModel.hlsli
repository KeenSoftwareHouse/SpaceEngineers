#ifndef LIGHTING_MODEL_H__
#define LIGHTING_MODEL_H__

#include <Frame.hlsli>
#include <GBuffer/Surface.hlsli>
#include <Math/Color.hlsli>
#include "Brdf.hlsli"

float falloff_to_radius(float f, float range) 
{
	float r_ = 0.333933902326 * sqrt(6.12430005749 + 5.9892092 * f) - 0.814156676978;
	return range * r_;
}

float3 calculate_light(SurfaceInterface surface, float3 lightVector, float3 lightColor, float sFactor, float dFactor)
{
	float3 N = surface.N;
	float3 V = surface.V;
	float3 H = normalize(V + lightVector);

    float3 lightRadiance = lightColor * MaterialRadiance(surface.albedo, surface.f0, surface.gloss * sFactor, N, lightVector, V, H, dFactor);
	return lightRadiance;
}

float3 main_directional_light(SurfaceInterface surface)
{
    return calculate_light(surface, -frame_.Light.directionalLightVec, frame_.Light.directionalLightColor, 
        frame_.Light.directionalLightGlossFactor, frame_.Light.directionalLightDiffuseFactor);
}

float3 env_main_directional_light(SurfaceInterface surface, float intensity)
{
    return calculate_light(surface, -frame_.Light.directionalLightVec, frame_.Light.directionalLightColor * intensity, 
        frame_.Light.directionalLightGlossFactor, frame_.Light.directionalLightDiffuseFactor);
}

float3 back_directional_light1(SurfaceInterface surface)
{
    return calculate_light(surface, -frame_.Light.backLightVec1, frame_.Light.backLightColor1, frame_.Light.backLightGlossFactor, 1.0f);
}

float3 back_directional_light2(SurfaceInterface surface)
{
    return calculate_light(surface, -frame_.Light.backLightVec2, frame_.Light.backLightColor2, frame_.Light.backLightGlossFactor, 1.0f);
}

// scattering

float uniform_fog(float dist, float density) 
{
	return 1 - exp(-dist * density);
}

float3 add_fog(float3 color, float dist, float3 ray, float3 viewer)
{
	dist = clamp(dist, 0, 1000);
	float c = frame_.Fog.mult;
	float b = frame_.Fog.density;

    float3 fog_color = srgb_to_rgb(D3DX_R8G8B8A8_UNORM_to_FLOAT4(frame_.Fog.color).xyz);
	float fog0 = c * uniform_fog(dist, b);

    return lerp( color, fog_color, fog0 );
}

#endif