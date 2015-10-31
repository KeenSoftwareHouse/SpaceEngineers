//Reflector light - coming from the camera/player
// Also used for spot lights
float3 ReflectorDirection;
float ReflectorConeMaxAngleCos;
float4 ReflectorColor;
float ReflectorRange;
float3 CameraPosition;
int ReflectorTextureEnabled;
float ReflectorIntensity;
float ReflectorFalloff;

Texture ReflectorTexture;
sampler ReflectorTextureSampler = sampler_state 
{ 
	texture = <ReflectorTexture>; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

//	Calculate attenuation factor of reflector light. Result will be in interval <0..1> and can be used to multiply diffuse/specular components.
float4 GetReflectorAttenuation(float distanceToReflectorInvertedNormalized, float3 directionToReflector)
{
	float actualAngle = 1 - dot(ReflectorDirection, -directionToReflector);	
	float4 ret = 6 * distanceToReflectorInvertedNormalized;

	//	Attenuate by cone angle
	ret *= 1 - saturate(actualAngle / ReflectorConeMaxAngleCos);
	ret = saturate(ret);
	
	//	Make the light not too bright, so it will be in interval <0..0,6>
    ret = ret * 0.6;
	return ret;
}


//	Compute distance from camera to vertex. This is then interpolated by pixel shader.
//	Notice: this value IS NOT in range <0..1>, but contains reall distance in meters.
float GetDistanceToReflector(float3 position)
{
	return length(CameraPosition - position);
}

//	Calculate the light direction ( from the surface to the light ), which is not normalized and is in world space
float3 GetDirectionToReflector(float3 position)
{
	return CameraPosition - position;
}


float3 GetReflectorAttenuation(float3 worldPosition)
{
	float distanceToReflector = GetDistanceToReflector(worldPosition);
	float3 directionToReflector = GetDirectionToReflector(worldPosition);
	float distanceToReflectorInverted = 1 - saturate(distanceToReflector / ReflectorRange);
	return GetReflectorAttenuation(distanceToReflectorInverted, normalize(directionToReflector));       
}

float3 GetReflectorColor(float3 worldPosition)
{
	return ReflectorColor * GetReflectorAttenuation(worldPosition);
}

float3 GetReflectorColor(float attenution)
{
	return ReflectorColor * attenution;
}