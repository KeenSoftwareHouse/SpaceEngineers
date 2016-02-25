static const int MAX_POINT_LIGHTS = 256;
static const int MAX_SPOTLIGHTS = 128;

#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#define FALOFF_TO_RADIUS

struct ProxyVertex
{
	float4 position : POSITION;
};

struct SpotlightConstants
{
	matrix  worldViewProj;
	matrix  shadowMatrix;

	float3 	position;
	float 	range;

	float3 	color;
	float 	apertureCos;

	float3	direction;
	float 	castsShadows;

	float3 	up;
    float __padding;
};

cbuffer Spotlights : register( b1 )
{
	SpotlightConstants Spotlight;
}

struct PointLightData {
	float3 positionView;
	float range;
    float3 color;
    float radius;
};

#ifndef MAX_TILE_LIGHTS
#define MAX_TILE_LIGHTS 256
#endif

StructuredBuffer<PointLightData> LightList : register ( t13 );
StructuredBuffer<uint> TileIndices : register( t14 );
Texture2D<float3> ReflectorMask : register( t13 );
Texture2D<float> SpotlightShadowmap : register( t14 );

#include <Lighting/LightingModel.h>
#include <postprocess_base.h>
#include <gbuffer.h>
#include <csm.h>
#include <EnvAmbient.h>
#include <vertex_transformations.h>

Texture2D<float> ShadowsMainView : register( MERGE(t,SHADOW_SLOT) );

float3 GetSunColor(float3 L, float3 V, float3 color, float sizeMult)
{
	return (saturate(color + 0.5f) + float3(0.5f, 0.35f, 0.0f)) * pow(saturate(dot(L, -V)), 4000.0f) * sizeMult;
}
