#include "../MyEffectShadowBase.fxh"

//	Dynamic light
struct DynamicLight 
{
    float3 Position;
    float4 Color;
    float Falloff;
    float Range;
};

//	Here we define array for holding lights with max size. No more light can be used, but less can. It depends on how many lights
//	are near object we want to draw and max number of light set by user in game options.
//	If you change length of this array, change it too in MyLightsConstants.MAX_LIGHTS_FOR_EFFECT
DynamicLight DynamicLights[8];

//	This number is set specificaly for every model or voxel cell and tells us how many light to use for lighting calculations
int DynamicLightsCount;


//	Color of sun light. This one will influence 'sun' factor comming from shadows.
float3 SunColor;
float SunIntensity;
float3 DirectionToSun = normalize(float3(0, 0, -1));
float3 AmbientColor;


//	Calculate the intensity of the light with exponential falloff
float GetDynamicLightBaseIntensity(DynamicLight light, float distance)
{
	return 1 - pow(saturate(distance / light.Range), light.Falloff);
}

//	Calculate point light with attenuation, diffuse and specular texture. Can be used for normal mapping too.
float4 CalculateDynamicLight_Diffuse(DynamicLight light, float3 position, float3 normal)
{
	float3 lightVector = light.Position - position;
	float lightDist = length(lightVector);
	float3 directionToLight = normalize(lightVector);

	float baseIntensity = GetDynamicLightBaseIntensity(light, lightDist);

	float diffuseIntensity = saturate( dot(directionToLight, normal));
	float4 diffuse = diffuseIntensity * light.Color;

	return baseIntensity * diffuse;
	//return float4(abs(directionToLight),1) * light.Color;
}


//	Calculate point light with attenuation for particle. Works with particle diffuse texture.
float3 CalculateDynamicLightForParticle(DynamicLight light, float3 position)
{
	float3 lightVector = light.Position - position;
	float lightDist = length(lightVector);
	// This wasn't used so I put it in comment
	//float3 directionToLight = normalize(lightVector);

	float baseIntensity = GetDynamicLightBaseIntensity(light, lightDist);

	return baseIntensity * light.Color.xyz;
}

float4 CalculateDynamicLight_Diffuse(float3 position, float3 normal)
{
	float4 lightColor = float4(0,0,0,0);

	for (int i = 0; i < DynamicLightsCount; i++)
	{
		lightColor += CalculateDynamicLight_Diffuse(DynamicLights[i], position, normal);
	}

	return lightColor;
}

float GetSunColor(float3 normal)
{
//	Sun - diffuse    
	float sunDiffuseMultiplier = saturate(dot(normal, DirectionToSun));
	return sunDiffuseMultiplier * SunColor * SunIntensity;
}