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
    float   glossFactor;
};

struct PointLightData 
{
	float3 positionView;
	float range;
    float3 color;
    float radius;
    
    float glossFactor;
    float _pad0, _pad1, _pad2;
};

#ifndef MAX_TILE_LIGHTS
#define MAX_TILE_LIGHTS 256
#endif
