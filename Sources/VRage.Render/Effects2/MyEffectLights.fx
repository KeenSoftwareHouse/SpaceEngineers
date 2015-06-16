#include "MyEffectBase.fxh"

float2 HalfPixel;
float3 LightPosition;
float LightRadius;
float LightIntensity;
float3 CameraPosition;
float3 LightColor;
float4x4 WorldViewProjMatrix;
float4x4 WorldMatrix;

//	Reflector light - coming from the camera/player
float3 ReflectorDirection;
float ReflectorConeMaxAngleCos;
float4 ReflectorColor;
float ReflectorRange;


Texture NormalsRT;
sampler NormalsRTSampler = sampler_state 
{ 
	texture = <NormalsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture DiffuseRT;
sampler DiffuseRTSampler = sampler_state 
{ 
	texture = <DiffuseRT> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 ScreenPosition : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjMatrix);
	output.ScreenPosition = output.Position;
	output.WorldPosition = mul(input.Position, WorldMatrix);
    return output;
}


struct CalculatedValues
{
    float3 Position;
    float3 ViewDir;
	float3 Normal;
	float3 Specular;
	float4 Diffuse;
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


float4 CalculateLighting(VertexShaderOutput input, out CalculatedValues values) : COLOR0
{
    float2 texCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);

	float4 encodedDepth = tex2D(DepthsRTSampler, texCoord);
	float emissive = encodedDepth.w;
	float viewDistanceNormalized = DecodeFloatRGBA(encodedDepth);
	float viewDistance = viewDistanceNormalized * FAR_PLANE_DISTANCE;
	values.ViewDir = normalize(input.WorldPosition - CameraPosition);

	float4 normal = tex2D(NormalsRTSampler, texCoord);
	float specularPower = normal.a * SPECULAR_POWER_RATIO;

	normal.xyz = GetNormalVectorFromRenderTarget(normal.xyz);
	values.Normal = normal;

    values.Diffuse = tex2D(DiffuseRTSampler, texCoord);

	float specularIntensity = values.Diffuse.a * SPECULAR_INTENSITY_RATIO;
	

	values.Position = values.ViewDir * viewDistance + CameraPosition;

	//surface-to-light vector
    float3 lightVector = LightPosition - values.Position;

	//reflection vector
    float3 reflectionVector = normalize(reflect(-lightVector, normal));

    //camera-to-surface vector
    float3 directionToCamera = normalize(CameraPosition - values.Position);

    //compute specular light
    float specularLight = specularIntensity * pow( saturate(dot(reflectionVector, directionToCamera)), specularPower);

    //compute attenuation based on distance - linear attenuation
    float attenuation = saturate(1.0f - length(lightVector)/LightRadius); 

    //normalize light vector
    lightVector = normalize(lightVector); 

    //compute diffuse light
    float NdL = max(0,dot(normal,lightVector));

    float3 diffuseLight = NdL * LightColor * values.Diffuse.rgb;

	values.Specular = specularLight.xxx * LightSpecularColor;

    //take into account attenuation and lightIntensity.
    return  float4(attenuation * (LightIntensity * diffuseLight + values.Specular), 1);
    
	//return  float4(specularLight.xxx, attenuation);
	//return  float4(LightIntensity * diffuseLight + float3(0,0,0), attenuation);
	//return float4(specularLight,specularLight,specularLight,attenuation);
	//return float4(emissive,emissive,emissive,1);
}


float4 PSLighting(VertexShaderOutput input) : COLOR0
{
	CalculatedValues values;
	float4 color = CalculateLighting(input, values);
	return color;
}

float4 PSLightingWithReflector(VertexShaderOutput input) : COLOR0
{
	CalculatedValues values;
    float4 color = CalculateLighting(input, values);

	float3 directionToReflector = -values.ViewDir;
	 //	Reflector - diffuse
    float reflectorDiffuseMultiplier = saturate(dot(values.Normal, directionToReflector));
   
   	float distanceToReflector = GetDistanceToReflector(values.Position);
	float distanceToReflectorInverted = 1 - saturate(distanceToReflector / ReflectorRange);

	//	Reflector - attenuation
    float4 reflectorAttenuation = GetReflectorAttenuation(distanceToReflectorInverted, directionToReflector);       

    //take into account attenuation and lightIntensity.
	float4 colorRefl = float4(reflectorAttenuation * (ReflectorColor * reflectorDiffuseMultiplier * values.Diffuse.xyz + values.Specular), 1);

	return  color + colorRefl;

	//return  float4(specularLight.xxx, attenuation);
	//return  float4(LightIntensity * diffuseLight + float3(0,0,0), attenuation);
	//return float4(specularLight,specularLight,specularLight,attenuation);
	//return float4(emissive,emissive,emissive,1);
}

float4 PixelShaderFunction_SpecularIntensity(VertexShaderOutput input) : COLOR0
{
	float2 texCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);
    float4 diffuse = tex2D(DiffuseRTSampler, texCoord);
	float specularIntensity = diffuse.a * SPECULAR_INTENSITY_RATIO;

	float4 encodedDepth = tex2D(DepthsRTSampler, texCoord);
	float emissive = encodedDepth.w;
	float viewDistanceNormalized = DecodeFloatRGBA(encodedDepth);
	float viewDistance = viewDistanceNormalized * FAR_PLANE_DISTANCE;

	float3 viewDir = normalize(input.WorldPosition - CameraPosition);
	float3 position = viewDir * viewDistance + CameraPosition;

	//surface-to-light vector
    float3 lightVector = LightPosition - position;

    //compute attenuation based on distance - linear attenuation
    float attenuation = saturate(1.0f - length(lightVector)/LightRadius); 


	return float4(specularIntensity, specularIntensity, specularIntensity, attenuation);
}

float4 PixelShaderFunction_SpecularPower(VertexShaderOutput input) : COLOR0
{
	float2 texCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);
	float4 normal = tex2D(NormalsRTSampler, texCoord);
	float specularPower = normal.a;

	float4 encodedDepth = tex2D(DepthsRTSampler, texCoord);
	float emissive = encodedDepth.w;
	float viewDistanceNormalized = DecodeFloatRGBA(encodedDepth);
	float viewDistance = viewDistanceNormalized * FAR_PLANE_DISTANCE;

	float3 viewDir = normalize(input.WorldPosition - CameraPosition);
	float3 position = viewDir * viewDistance + CameraPosition;

	//surface-to-light vector
    float3 lightVector = LightPosition - position;

    //compute attenuation based on distance - linear attenuation
    float attenuation = saturate(1.0f - length(lightVector)/LightRadius); 

	return float4(specularPower, specularPower, specularPower, attenuation);
}

technique Technique_Lighting
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSLighting();
    }
}

technique Technique_LightingWithReflector
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PSLightingWithReflector();
    }
}

technique Technique_ShowSpecularIntensity
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_SpecularIntensity();
    }
}


technique Technique_ShowSpecularPower
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction_SpecularPower();
    }
}