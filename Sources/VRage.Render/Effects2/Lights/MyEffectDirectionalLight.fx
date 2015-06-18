#include "../MyEffectShadowBase.fxh"

#define SHADOW_FILTERING 5
 

uniform int EnableCascadeBlending = 1;
uniform int EnableAmbientEnv = 1;
uniform int EnableReflectionEnv = 1;					     


float4 HalfPixelAndScale;

float NearSlopeBiasDistance;

float3 CameraPosition; 


float2 ShadowHalfPixel;


float4x4 WorldViewProjMatrix;
float4x4 CameraMatrix;

float3 FrustumCorners[4];


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
	magfilter = POINT; 
	minfilter = POINT; 
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

TextureCube TextureEnvironmentMain;
sampler TextureEnvironmentMainSampler = sampler_state
{
	texture = <TextureEnvironmentMain>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = NONE;
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

TextureCube TextureEnvironmentAux;
sampler TextureEnvironmentAuxSampler = sampler_state
{
	texture = <TextureEnvironmentAux>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = NONE;
	AddressU = CLAMP; 
	AddressV = CLAMP;
};



struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 TexCoordAndCornerIndex	: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float4 ScreenPosition : TEXCOORD1;
	float3 FrustumCorner : TEXCOORD2; 
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = (input.TexCoordAndCornerIndex.xy + HalfPixelAndScale.xy) * HalfPixelAndScale.zw;
	output.ScreenPosition = input.Position;
	output.FrustumCorner = FrustumCorners[input.TexCoordAndCornerIndex.z];
	return output;
}


struct CalculatedValues
{
	float3 Position;
	float3 Normal;
	float3 Specular;
	float4 Diffuse;
};

float4 SampleEnvironmentTexture(float3 texCoord)
{
	float4 mainColor = texCUBE(TextureEnvironmentMainSampler, texCoord);
	float4 auxColor = texCUBE(TextureEnvironmentAuxSampler, texCoord);
	float4 blendedColor = lerp(mainColor, auxColor, TextureEnvironmentBlendFactor);
	return blendedColor;
}

float4 CalculateLighting(VertexShaderOutput input, out CalculatedValues values, uniform int shadowsDisabled, uniform int lightingEnabled,float4 screenPos : VPOS) : COLOR0
{
	float4 encodedDepth = tex2D(DepthsRTSampler, input.TexCoord);

	float fSceneDepthNorm = DecodeFloatRGBA(encodedDepth);

	//if, else, discard
	if (fSceneDepthNorm >= 0.99999f)
	{
		return float4(0,0,0,0);
	}

	float emissive;
	float reflection;
	UnpackGBufferEmissivityReflection(encodedDepth.w, emissive, reflection);

	// Get viewspace position
	float4 vPositionVS = float4(GetViewPositionFromDepth(fSceneDepthNorm, input.FrustumCorner.xyz), 1);

	float4 worldPosition = mul(vPositionVS, CameraMatrix);
	values.Position = worldPosition.xyz;
	
	float4 normal = tex2D(NormalsRTSampler, input.TexCoord);
	float specularPower = normal.a * SPECULAR_POWER_RATIO;

	normal.xyz = GetNormalVectorFromRenderTarget(normal.xyz);
	float blend = length(normal.xyz);
	normal.xyz = normalize(normal.xyz);
	values.Normal = normal.xyz;

	values.Diffuse = tex2D(DiffuseRTSampler, input.TexCoord);

	float specularIntensity = values.Diffuse.a * SPECULAR_INTENSITY_RATIO;
	
	//compute diffuse light
	float NdLbase = dot(normal.xyz, -LightDirection);
	float NdL = max(0,NdLbase);
	float3 diffuseLight = NdL * LightColorAndIntensity.xyz * values.Diffuse.rgb;

	//compute back diffuse light
	float backNdL = max(0,-NdLbase);
	float3 backDiffuseLight = backNdL * BacklightColorAndIntensity.xyz * values.Diffuse.rgb;

	//reflection vector
	float3 reflectionVector = -(reflect(-LightDirection, normal.xyz));
	float3 reflectionVectorBack = -(reflect(LightDirection, normal.xyz));

	//camera-to-surface vector
	float3 directionToCamera = normalize(CameraPosition - worldPosition.xyz);


	float3 shadows = 0;

	if (dot(diffuseLight, 1) + specularIntensity > 0)
	{
		//Sun shadows
		float diff = 0;
		float length0 = length(vPositionVS.xyz);
		//float bias = (length0 < NearSlopeBiasDistance) ? 20.0f : 1 - NdL; //cockpit
		float bias = 1 - NdL; 

		float3 fShadowTerm1 = GetShadowTermFromPosition(vPositionVS, -length0, SHADOW_FILTERING, bias, diff);
		shadows = fShadowTerm1;

		if (EnableCascadeBlending > 0)
		{
			float blendDiff = length0 * 0.2f;
			float testDepth = length0 - blendDiff;
	
			float3 fShadowTerm2 = GetShadowTermFromPosition(vPositionVS, -testDepth, SHADOW_FILTERING, bias, diff);
			float blend = saturate(-diff / blendDiff);
		
			shadows = lerp(fShadowTerm1, fShadowTerm2, blend);
		} 
	}
	  /*
	else
	{
		return float4(1,0,0,1);
		discard;
	} 	*/


	values.Specular = float3(0,0,0);
	//specularIntensity = 0.8f;
	//specularPower = 2.0f;

	float specularLight = 0;	
	if ((shadows.x + specularIntensity) > 0)
	{
		//compute specular light
		float specularLight = specularIntensity * pow( saturate(dot(reflectionVector, directionToCamera)), specularPower);
		//values.Specular = specularLight.xxx * LightSpecularColor * lerp(LightSpecularColor, values.Diffuse.rgb, 0.5);
		values.Specular = specularLight.xxx * LightSpecularColor;
		//values.Specular = float3(1,0,0);
	}

	float backSpecular = specularIntensity * pow( saturate(dot(reflectionVectorBack, directionToCamera)), specularPower) * 0.5f;
	backSpecular = backSpecular.xxx * float3(1,1,1) * lerp(float3(1,1,1), values.Diffuse.rgb, 0.5);

	float3 ambientTexCoord = -normal.xyz;
	float4 ambientSample = SampleAmbientTexture(ambientTexCoord);
	float3 ambientColor = AmbientMinimumAndIntensity.w * ambientSample.xyz * EnableAmbientEnv;
	float3 finalAmbientColor =  max(ambientColor, AmbientMinimumAndIntensity.xyz) * values.Diffuse.rgb;

	float4 lightColor =  float4(((LightColorAndIntensity.w * (diffuseLight + values.Specular))) * max(shadows, shadowsDisabled), 1) * lightingEnabled;
	lightColor += float4(finalAmbientColor + BacklightColorAndIntensity.w * (backDiffuseLight + backSpecular) * ambientSample.w, 1);
	float4 result = lightColor;

	if ((reflection > 0) && (specularIntensity > 0))
	{
		//float reflectionCoeficient = saturate(specularIntensity) * max(reflection * 0.0f, VoxelReflectionMultiplier) * EnableReflectionEnv;
		float reflectionCoeficient = saturate(specularIntensity * 0.1f) * reflection * EnableReflectionEnv;
		//reflectionCoeficient = min(0.5f, reflectionCoeficient); //becase full mirror can get became dark

		float3 reflectedEyeDirection = -reflect(-directionToCamera, normal.xyz);
		float4 reflectionColor = SampleEnvironmentTexture(reflectedEyeDirection);

		result = lerp(lightColor, reflectionColor, reflectionCoeficient);
	}

	result.a = specularLight; //strength for HDR
	//result.xyz = NdL.xxx;
	// Alpha must be clamped to ensure proper blending
	result.a = saturate(result.a);
	return blend * result;
}

float4 PSLighting(VertexShaderOutput input, uniform int shadowsDisabled, uniform int lightingEnabled,float4 screenPos : VPOS) : COLOR0
{
	CalculatedValues values;
	float4 color = CalculateLighting(input, values, shadowsDisabled, lightingEnabled, screenPos);
	return color;
	//return float4(1,0,0,1);
}

float4 PixelShaderFunction_SpecularIntensity(VertexShaderOutput input) : COLOR0
{
	float2 texCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixelAndScale.xy) * HalfPixelAndScale.zw;
	float4 diffuse = tex2D(DiffuseRTSampler, texCoord);
	float specularIntensity = diffuse.a * SPECULAR_INTENSITY_RATIO;

	return float4(specularIntensity, specularIntensity, specularIntensity, 1);
}

float4 PixelShaderFunction_SpecularPower(VertexShaderOutput input) : COLOR0
{
	float2 texCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixelAndScale.xy) * HalfPixelAndScale.zw;
	float4 normal = tex2D(NormalsRTSampler, texCoord);
	float specularPower = normal.a;

	return float4(specularPower, specularPower, specularPower, 1);
}

technique Technique_Lighting
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PSLighting(0, 1);
	}
}

technique Technique_NoLighting
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PSLighting(0, 0);
	}
}

technique Technique_LightingWithoutShadows
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PSLighting(1, 1);
	}
}
