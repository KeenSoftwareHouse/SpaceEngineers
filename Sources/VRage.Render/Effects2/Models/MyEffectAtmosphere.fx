#include "../MyEffectDynamicLightingBase.fxh"
#include "../MyEffectInstanceBase.fxh"
#include "../MyEffectWorldPosition.fxh"
#include "../MyEffectAtmosphereBase.fxh"

//	This shader renders a model with diffuse & specular & normal map textures, so it requires certain vertex shader data

float4x4	WorldMatrix;
float4x4	ViewMatrix;
float4x4	ProjectionMatrix;



Texture SourceRT;
sampler SourceRTSampler = sampler_state
{
	texture = <SourceRT>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = NONE;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

VertexShaderOutputWorldPosition VertexShaderFunction_Atmosphere(VertexShaderInput_DNS input)
{
	VertexShaderOutputWorldPosition output;

	input.BaseInput.Position = normalize(UnpackPositionAndScale(input.BaseInput.Position));

	output = (VertexShaderOutputWorldPosition)0;
	output.WorldPosition = input.BaseInput.Position;
	output.Position = mul(input.BaseInput.Position, WorldMatrix);
	output.ViewPosition= output.Position = mul(output.Position, ViewMatrix);
	output.Position = mul(output.Position, ProjectionMatrix);
	output.ScreenPosition = output.Position;
	return output;
}

float PhaseHenyeyGreenstein(float cosTheta)
{
	return (1.0 - g2) / pow(1.0 + g2 - 2.0*g*cosTheta, 1.5);
}

float4 PixelShaderFunction_Sky(VertexShaderOutputWorldPosition input) :COLOR0
{
	float3 v3Pos = input.WorldPosition.xyz;
	float3 v3Ray = normalize(v3Pos)*OuterRadius - CameraPos;
	float fFar = length(v3Ray);
	v3Ray /= fFar;

	float3 v3Start = float3(0, 0, 0);
	float fStartOffset = 0;

	if (IsInAtmosphere)
	{
		v3Start = CameraPos;
		float fHeight = length(v3Start);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - CameraHeight));
		float fStartAngle = dot(v3Ray, v3Start) / fHeight;
		fStartOffset = fDepth*ScaleFun(fStartAngle);
	}
	else
	{
		float B = 2.0 * dot(CameraPos, v3Ray);
		float C = CameraHeight2 - OuterRadius2;
		float fDet = max(0.0, B*B - 4.0 * C);
		float fNear = 0.5 * (-B - sqrt(fDet));

		// Calculate the ray's starting position, then calculate its scattering offset
		v3Start = CameraPos + v3Ray * fNear;
		fFar -= fNear;
		float fStartAngle = dot(v3Ray, v3Start) / OuterRadius;
		float fStartDepth = exp(-1.0 / ScaleDepth);
		fStartOffset = fStartDepth*ScaleFun(fStartAngle);
	}

	float fSampleLength = fFar / Samples;
	float fScaledLength = fSampleLength * ScaleAtmosphere;
	float3 v3SampleRay = v3Ray * fSampleLength;
	float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;

	float3 v3FrontColor = float3(0.0, 0.0, 0.0);
 	for (int i = 0; i < NumSamples; i++)
 	{
 		float fHeight = length(v3SamplePoint);
		float fHeigtInv = 1 / fHeight;
 		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - fHeight));
		float fLightAngle = dot(LightPosition, v3SamplePoint) * fHeigtInv;
		float fCameraAngle = dot(v3Ray, v3SamplePoint) * fHeigtInv;
 		float fScatter = (fStartOffset + fDepth*(ScaleFun(fLightAngle) - ScaleFun(fCameraAngle)));
 		float3 v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
 		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
 		v3SamplePoint += v3SampleRay;
 	}

	float3 colorMie = v3FrontColor * KmESun;
	float3 colorRayleigh = v3FrontColor * (InvWavelength * KrESun);

	float thetaCos = dot(LightPosition, -v3Ray);
	float thetaCosR = -thetaCos * 0.5 + 0.5;
	float phaseRayleigh = 0.75 * (1.0 + thetaCosR*thetaCosR);
	float phaseMie = PhaseHenyeyGreenstein(thetaCos);

	float3 outRayleigh = phaseRayleigh * colorRayleigh;
	float3 outMie = phaseMie      * colorMie;

	return  float4(outRayleigh + outMie,0.9);
}

float4 PixelShaderFunction_Surface(VertexShaderOutputWorldPosition input) :COLOR0
{
	CalculatedWorldValues values;
	LoadWorldValues(input, values);
	float4 color = tex2D(SourceRTSampler, values.TexCoord);
	return CalculateAtmosphere(values.Position, CameraPos, color.rgb);
}

technique Technique_RenderQualityNormal
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction_Atmosphere();
		PixelShader = compile ps_3_0 PixelShaderFunction_Sky();
	}
}

technique Technique_Surface
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction_Atmosphere();
		PixelShader = compile ps_3_0 PixelShaderFunction_Surface();
	}
}