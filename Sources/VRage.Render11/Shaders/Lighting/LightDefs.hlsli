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
    float   _pad1;

    float   glossFactor;
    float   diffuseFactor;
    float   _pad2, _pad3;

    float3 	up;
    float 	apertureCos;

    float3	direction;
    float 	castsShadows;
};

struct PointLightData 
{
	float3 positionView;
	float range;

    float3 color;
    float radius;
    
    float glossFactor;
    float diffuseFactor;
    float _pad1, _pad2;
};

#ifndef MAX_TILE_LIGHTS
#define MAX_TILE_LIGHTS 256
#endif
