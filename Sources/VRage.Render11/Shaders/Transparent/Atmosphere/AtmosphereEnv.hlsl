// @define SAMPLE_FREQ_PASS

#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#include "AtmosphereCommon.hlsli"
#include <frame.hlsli>

Texture2D<float>	DepthBuffer	: register( t0 );

cbuffer Constants : register( MERGE(b,OBJECT_SLOT) )
{
	matrix viewMatrix;
	matrix projMatrix;
    float3 directionalLightVec;
    float __pad;
};

float2 ep_screen_to_uv(float2 screencoord)
{
    const float2 invres = 1.0f / 256;
    return screencoord * invres + invres / 2;
}

float3 ep_compute_screen_ray(float2 uv, matrix projMatrix)
{
    const float ray_x = 1.0f / projMatrix._11;
    const float ray_y = 1.0f / projMatrix._22;
    return float3(lerp(-ray_x, ray_x, uv.x), -lerp(-ray_y, ray_y, uv.y), -1.);
}

float3 ep_view_to_world(float3 view, matrix viewMatrix)
{
    return mul(view, (float3x3) viewMatrix);
}

float ep_compute_depth(float hw_depth, matrix projMatrix)
{
    return -linearize_depth(hw_depth, projMatrix);
}

float3 originalV(float2 uv)
{
	const float ray_x = 1;
	const float ray_y = 1;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, uv.x ), -lerp( -ray_y, ray_y, uv.y ), -1.);

	return mul(screen_ray, transpose((float3x3)viewMatrix));
}

// additive blend
void __pixel_shader(float4 svPos : SV_Position, out float4 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	) 
{
    float2 screencoord = svPos.xy;
	float2 uv = ep_screen_to_uv(screencoord);
    float3 screen_ray = ep_compute_screen_ray(uv, projMatrix);
    float3 Vinv = ep_view_to_world(screen_ray, transpose(viewMatrix));
    
    float native_depth = DepthBuffer[screencoord].r;
    float depth = ep_compute_depth(native_depth, projMatrix);
    float3 V = -normalize(Vinv);
    float3 position = depth * Vinv;

	// We need to clamp it to mitigate artefacts in the tonemapper
	// 10 is the smallest value for which it still looks acceptable
    float4 m = float4(frame_.Light.envAtmosphereBrightness.xxx, 1);
    float4 atmoColor = ComputeAtmosphere(V, position, directionalLightVec, depth, native_depth, 4);
    float4 result = float4(atmoColor.xyz * frame_.Light.envAtmosphereBrightness.xxx, 1 - (1 - atmoColor.w) * frame_.Light.envAtmosphereBrightness.x);
    output = result;//frame_.Light.envAtmosphereBrightness.xxx, 1);
}
