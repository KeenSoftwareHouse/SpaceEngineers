#include <Math/Math.hlsli>
#include <Common.hlsli>

Texture2D<float2> DensityLut : register(t5);

cbuffer AtmosphereConstants : register(b1)
{
    float3  PlanetCenter;
    float   RadiusAtmosphere;

    float3  BetaRayleighScattering;
    float   RadiusGround;

    float3  BetaMieScattering;
    float   MieG;

    float2  HeightScaleRayleighMie;
    float	PlanetScaleFactor;
    float   AtmosphereScaleFactor;

    float   Intensity;
    float   FogIntensity;
    float2  __padding;

    matrix  WorldViewProj;
};


void GetRaySphereIntersection(float3 origin, float3 direction, float3 center, float radius, out float2 intersections)
{
	origin -= center;
	float A = dot(direction, direction);
	float B = 2.0f * dot(origin, direction);
	float C = dot(origin, origin) - radius * radius;
    float D = B * B - 4.0f * A * C;
    if(D >= 0.0f)
    {
		D = sqrt(D);
		intersections = float2(-B - D, -B + D) / (2.0f * A);
    }
    else
    {
		intersections = -1.0f;
    }
}

float PhaseRayleigh(float cosTheta) 
{
	return 0.75f + 0.75f * cosTheta * cosTheta;
}

float PhaseMie(float cosTheta) 
{
    const float g = MieG;
    const float gSq = g*g;
    return mad(1.5f, -gSq, 1.5f) * (1 + cosTheta * cosTheta) / (2.0f + gSq) / max(pow(abs(1.0f + gSq - 2.0f * g * cosTheta), 1.5f), 0.0001);
}

float3 GetPlanetCenter()
{
	return PlanetCenter / PlanetScaleFactor;
}

float GetPlanetRadius()
{
	return RadiusGround * 1.01f;
}

float2 GetOpticalDepth(float height)
{
	return exp(-(height / HeightScaleRayleighMie));
}

// Assumes P1 is inside atmosphere and P2 is on top of the atmosphere
float2 GetUvFromPoints(float3 P1, float3 P2)
{
	// X is height
	// Y is zenith angle

    float height = (length(P1 - GetPlanetCenter()) - RadiusGround) / AtmosphereScaleFactor / (RadiusAtmosphere - RadiusGround);
    float cosTheta = mad(0.5f, dot(normalize(P1 - GetPlanetCenter()), normalize(P2 - P1)), 0.5f);

	return float2(height, cosTheta);
}

// P1 is straight up (Y = 1)
// P2 is in the X direction
void GetPointsFromUv(float2 uv, out float3 P1, out float3 P2)
{
	float height = uv.x * (RadiusAtmosphere - RadiusGround) * AtmosphereScaleFactor + RadiusGround;
	P1 = GetPlanetCenter() + float3(0, 1, 0) * height;


	float Ra = RadiusAtmosphere;
	float Rp = height;
    float cosTheta = 2.0f*uv.y - 1.0f;
	float sinTheta = 1.0f - sqrt(cosTheta * cosTheta);

	float p = Rp * cosTheta;
	float q = Rp * Rp - Ra * Ra;

    float Rc = (-p + sqrt(p * p - q));

	P2 = float3(Rc * sinTheta, mad(Rc, cosTheta, Rp), 0.0f) + GetPlanetCenter();
}

float2 ComputeOpticalDepth(float3 P1, float3 P2, int steps)
{
	float3 planetCenter = GetPlanetCenter();

	float startHeight = length(P1 - planetCenter) - RadiusGround;

	float rayLength = length(P1 - P2);
	if (rayLength < 0.01f)
	{
		return 0;
	}
	float3 rayDir = normalize(P2 - P1);
    float2 totalOpticalDepth = GetOpticalDepth(startHeight) * (rayLength / steps);
	float prevPartialRayLength = 0.0f;

	for (int i = 1; i <= steps; i++)
	{
		float partialRayLength = (rayLength / steps) * i;
		float3 P = mad(partialRayLength, rayDir, P1);

		float stepLength = partialRayLength - prevPartialRayLength;
		prevPartialRayLength = partialRayLength;

		float height = (length(P - planetCenter) - RadiusGround) / AtmosphereScaleFactor;
		float2 opticalDepth = GetOpticalDepth(height);

        totalOpticalDepth += 2.0f*stepLength*opticalDepth;
	}

	return 0.5f * totalOpticalDepth;
}

#ifndef ATMOSPHERE_PRECOMPUTE
float2 GetOpticalDepthFromTexture(float3 P1, float3 P2)
{
	float2 uv = GetUvFromPoints(P1, P2);
	return DensityLut.SampleLevel(LinearSampler, uv, 0);
}


float3 ComputeTransmittance(float3 P1, float3 P2, int steps)
{
	if (length(P1 - P2) < 1.0f)
	{
		return 1;
	}
	float2 opticalDepth = GetOpticalDepthFromTexture(P1, P2);
	float3 transmittance = BetaRayleighScattering * opticalDepth.x + (BetaMieScattering * 0.9) * opticalDepth.y;
	return exp(-transmittance);
}

float GetRayLength(int i, int steps)
{
	// Exponential
	// We take more samples far from the camera
    return 1.0f - exp2(-i);
}

//float4 ComputeAtmosphere(SurfaceInterface input)
float4 ComputeAtmosphere(float3 inputV, float3 position, float3 lightVec, float depth, float native_depth, int steps)
{
	const float3 V = -normalize(inputV);
    const float3 L = -normalize(lightVec);

	float3 planetCenter = GetPlanetCenter();
	float2 viewAtmosphereInt;
	GetRaySphereIntersection(0.0f, V, planetCenter, RadiusAtmosphere, viewAtmosphereInt);

	float3 rayEnd = position / PlanetScaleFactor;
	float rayLength = length(rayEnd);
	float3 ray = rayEnd / rayLength;


	if (viewAtmosphereInt.y > 0)
		rayEnd = V * min(viewAtmosphereInt.y, rayLength);
	else
		discard;
	
	ray = normalize(rayEnd);
	rayLength = length(rayEnd);

	float cosTheta = dot(V, L);

	float3 preIntegralRayleigh = PhaseRayleigh(cosTheta) * BetaRayleighScattering / (4.0f * M_PI);
	float3 preIntegralMie = PhaseMie(cosTheta) * BetaMieScattering / (4.0f * M_PI);

	float3 Pa = 0;
	if (viewAtmosphereInt.x > 0)
	{
		Pa = ray * viewAtmosphereInt.x	* 1.001f;
	}

	float3 Pb = rayEnd * 0.999f;

	if (dot(Pa, Pa) > dot(Pb, Pb))
		discard;

	float3 totalInscatteringRayleigh = 0;
	float3 totalInscatteringMie = 0;
	float totalLength = length(Pa - Pb);
	float stepLength = totalLength / steps;
    float halfStepLength = 0.5f*stepLength;

	float3 prevScatteringRayleigh = 0;
	float3 prevScatteringMie = 0;

	float3 PaPTransmittance = 0;
	float2 prevOpticalDepth = 0;
	float prevRayLength = 0;

	for (int i = 0; i <= steps; i++)
	{
		float3 P = Pa + ray * stepLength * i;

		float2 lightAtmosphereInt;
		GetRaySphereIntersection(P, L, planetCenter, RadiusAtmosphere, lightAtmosphereInt);
		if (lightAtmosphereInt.y < 0)
			continue;

		float3 Pc = P + L * lightAtmosphereInt.y;

		float3 inscatteringRayleigh;
		float3 inscatteringMie;

		float height = max((length(P - planetCenter) - RadiusGround) / AtmosphereScaleFactor, 0);
			
		float2 opticalDepth = GetOpticalDepth(height);
		if (i > 0)
		{
            float2 totalOpticalDepth = (opticalDepth + prevOpticalDepth) * halfStepLength;
			float3 transmittance = BetaRayleighScattering * totalOpticalDepth.x + (BetaMieScattering * 0.9) * totalOpticalDepth.y;
			PaPTransmittance += transmittance;
		}

		float3 transmittance = ComputeTransmittance(P, Pc, 10) * exp(-PaPTransmittance);
		inscatteringRayleigh = (opticalDepth.x * transmittance);
		inscatteringMie = (opticalDepth.y * transmittance);

		if (i > 0)
		{
            totalInscatteringRayleigh += (inscatteringRayleigh + prevScatteringRayleigh) * halfStepLength;
            totalInscatteringMie += (inscatteringMie + prevScatteringMie) * halfStepLength;
		}

		prevScatteringRayleigh = inscatteringRayleigh;
		prevScatteringMie = inscatteringMie;
		prevOpticalDepth = opticalDepth;
	}

    float3 rayleigh = preIntegralRayleigh * totalInscatteringRayleigh;
    float rayleighStrength = length(rayleigh);
    float3 mie = preIntegralMie * totalInscatteringMie;
    float mieStrength = length(mie);

    // Destination color removal (to hide the skybox with fully lit atmosphere)
    // Calculated Radius for coverage; should be in the data, always in range (ground, atmosphere)
    float radiusCoverage = RadiusGround + (RadiusAtmosphere - RadiusGround) * 5.0f;
    // Calculate current height between ground and coverage radius
    float heightFactor = saturate((length(planetCenter) - RadiusGround) / (radiusCoverage - RadiusGround));
    // apply current rayleigh strength - so in the night we can see the skybox; rayleighCoverageFactor should be in the data
    float rayleighCoverageFactor = 100;
    float destAlpha = 1 - (1 - heightFactor) * saturate(rayleighStrength * rayleighCoverageFactor);

	// Remove mie scattering when overlapping with close objects
	// For far objects it will be removed when taking into consideration the entire shape of the earth
	float t = 1.0f, tc = 7.0f;
	if (depth_not_background(native_depth))
	{
		// Scale down fog for close objects
		//t = saturate((depth - (RadiusGround / 5000.0f)) / (RadiusGround / 1000.0f));
		t = saturate((depth - 1000) / 1000.0f) * FogIntensity;
        tc = t;
		mie = 0;
        // no coverage of the destination pixels
        destAlpha = saturate(rcp(length(PaPTransmittance) * tc));
	}
    // old calculation based on the transmittance
    //float destAlpha = saturate(rcp(length(PaPTransmittance) * tc));
    
    float3 integral = (rayleigh + mie);
	float atmosphereBoost = t * 2.5f * Intensity;
    
    return float4(integral * atmosphereBoost, destAlpha);
}
#endif