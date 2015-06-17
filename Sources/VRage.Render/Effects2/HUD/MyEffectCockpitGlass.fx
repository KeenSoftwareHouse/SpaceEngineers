#include "../MyEffectDynamicLightingBase.fxh"

float4		GlassDirtLevelAlpha;
float2		HalfPixel;
float4x4    WorldViewProjectionMatrix;
float4x4    WorldMatrix;
float4x4    ViewMatrix;
float3		CockpitInteriorLight;
float3		ReflectorPosition;

//	Near light (fake reflector light) - coming from the camera/player
//	Used for lighting near voxels and guns visible from the cockpit. It's also nice when guns are shoting.
float		NearLightRange;
float4		NearLightColor;




Texture CockpitGlassTexture;
sampler CockpitGlassTextureSampler = sampler_state 
{ 
	texture = <CockpitGlassTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
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
	float4 PositionAndAlpha : POSITION0;
	float4 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};
struct VertexShaderOutput
{
	float4 Position : POSITION;
	float3 TexCoordAndAlpha : TEXCOORD0;
	float4 ScreenPosition : TEXCOORD1;
	float3 ViewPosition : TEXCOORD2;
	float3 LightAndSunShadowColor : TEXCOORD3;
};



//	This light is here only because I want dynamic light on cockpit, even if in total dark. But I don't want big differences, so that's why here is LERP.
//	This light is connected to ship reflector or 'near light'. Thus if reflector is off or player isn't shooting, it's black.
float GetCockpitLightDiffuseMultiplier(float3 normal, float3 directionToReflector)
{
    //float cockpitLightDiffuseMultiplier = saturate(dot(normal, directionToReflector));
    //cockpitLightDiffuseMultiplier = lerp(0.25, 0.75, cockpitLightDiffuseMultiplier);

    return 0.3 + 0.4 * saturate(dot(normal, directionToReflector));
}



//	Sun - diffuse (see that I use ABS for the angle, because I want to have sun affecting the glass from both sides
float GetSunDiffuseMultiplierForCockpitGlassAndDecals(float3 normal)
{
	return saturate(abs(dot(normal, DirectionToSun)));
}




VertexShaderOutput VS_GlassDefault(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 Position = float4(input.PositionAndAlpha.xyz, 1);
	output.TexCoordAndAlpha.z = input.PositionAndAlpha.w;
	input.Normal = UnpackNormal(input.Normal);
	
	output.Position = mul(Position, WorldViewProjectionMatrix);
	output.ScreenPosition = output.Position;
	output.TexCoordAndAlpha.xy = input.TexCoord;

	input.Normal = input.Normal;

	//*-1 becuse of inverted normals for dynamic lights
	float3 normal = mul(input.Normal * -1, WorldMatrix);
	float3 worldPosition = mul(Position, WorldMatrix);
	
	float3 directionToReflector = normalize(ReflectorPosition - worldPosition);
	output.ViewPosition = mul(worldPosition, ViewMatrix);

	//	Sun - diffuse (see that I use ABS for the angle, because I want to have sun affecting the glass from both sides
	float sunDiffuseMultiplier = GetSunDiffuseMultiplierForCockpitGlassAndDecals(normal);
	
	//	This light is connected to ship reflector or 'near light'. Thus if reflector is off or player isn't shooting, it's black.
    float cockpitLightDiffuseMultiplier = GetCockpitLightDiffuseMultiplier(normal, directionToReflector);
	float cockpitLight = CockpitInteriorLight.xyz * cockpitLightDiffuseMultiplier + NearLightColor;

//	Add dynamic lights - this is not neceserary for cockpit glass, it just tried it. May be disabled if you need optimizations.
	float3 dynamicLight = 0;
    for (int i = 0; i < DynamicLightsCount; i++)
    {
		dynamicLight += CalculateDynamicLight_Diffuse(DynamicLights[i], worldPosition, normal).xyz;
    }
    
	float diff = 0;
#ifdef COLOR_SHADOW_MAP_FORMAT
	float3 sunShadow = 1.0f;
#else
	float3 sunShadow = GetShadowTermFromPosition(float4(output.ViewPosition, 1), output.ViewPosition.z, 3, 0, diff);
#endif
	
	output.LightAndSunShadowColor = SunColor.xyz * /*sunDiffuseMultiplier*/ saturate(sunShadow) /*+ cockpitLight */+ dynamicLight;
	
	return output;
}



float GetNearLightAttenuation(float distanceToReflector)
{
    //	This light's range depends on normalized distance, so here we take it from it
	//	Make the light not too bright, so it will be in interval <0..0,8>
    return (1 - saturate(distanceToReflector / NearLightRange)) * 0.8;
}



float4 PS_GlassDefault(VertexShaderOutput input) : COLOR0
{	
    float2 screenSpaceTexCoord = GetScreenSpaceTextureCoord(input.ScreenPosition, HalfPixel);

	float mistAlpha = 0;

    float depthBufferDistance = DecodeFloatRGBA(tex2D(DepthsRTSampler, screenSpaceTexCoord)) * FAR_PLANE_DISTANCE;    

	float viewDistance = -input.ViewPosition.z;

	if (viewDistance > depthBufferDistance)
	{
		discard;
		//return float4(1,1,1,1);
	}

	mistAlpha = saturate((depthBufferDistance - viewDistance) / 0.1f );

	//	Cockpit glass texture is only grayscale (for saving memory), thus we have to extract alpha component from it.
	float4 glassTexture = tex2D(CockpitGlassTextureSampler, input.TexCoordAndAlpha.xy);
	
	//	Create alpha from color and then increase it
	//glassTexture = dot(glassTexture, GlassDirtLevelAlpha);//1.5;//1.2;
	
	//	Decrease color of texture, we don't want dirt to be white
	//glassTexture.xyz *= 0.5;
	
	float4 result;
	
	//	Apply ambient + sun + near light
	result.xyz = glassTexture * (AmbientColor + input.LightAndSunShadowColor);
		
    //	Retain original alpha, not affected by lights
    result.w = glassTexture.w * mistAlpha;

	// Apply fadeint/out blending 
	//result.w = 0;// input.TexCoordAndAlpha.z;
	return result;
}



technique GlassDefault
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 VS_GlassDefault();
		PixelShader = compile ps_3_0 PS_GlassDefault();
	}
}
