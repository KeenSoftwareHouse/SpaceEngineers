
float ScaleDepth;
float CameraHeight;
float CameraHeight2;
float3 CameraPos;
float OuterRadius;
float OuterRadius2;
float InnerRadius;
float InnerRadius2;

float ScaleAtmosphere;
float ScaleOverScaleDepth;

bool IsInAtmosphere;

float3 LightPosition;
float3 InvWavelength;


float ScaleFun(float fCos)
{
	float x = 1.0 - fCos;
	return ScaleDepth * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
}

const static float PI = 3.14159f;
const static float Kr = 0.0025f;
const static float Km = 0.0010f;

const static float KrSun = 30;
const static float KrESun = Kr*KrSun;
const static float KmESun = Km*KrSun;
const static float Samples = 2;
const static float NumSamples = 3;
const static float g = -0.990f;
const static float g2 = g*g;
const static float Km4PI = Km * 4 * PI;
const static float Kr4PI = Kr * 4 * PI;
const static float ConversionToKm = 1 / 1000.0f;

float4 CalculateAtmosphere(float3 worldPosition, float3 offset, float3 color)
{
	float3 v3Pos = worldPosition*ConversionToKm + offset;
	float3 v3Ray = v3Pos - CameraPos;
	float fFar = length(v3Ray);
	v3Ray /= fFar;

	float3 v3Start = float3(0, 0, 0);
	float fCameraOffset = 0;

	float fCameraAngle = dot(-v3Ray, v3Pos) / length(v3Pos);
	float fLightAngle = dot(LightPosition, v3Pos) / length(v3Pos);
	float fCameraScale = ScaleFun(fCameraAngle);
	float fLightScale = ScaleFun(fLightAngle);
	float fTemp = (fLightScale + fCameraScale);

	if (IsInAtmosphere)
	{
		v3Start = CameraPos;
		float fDepth = exp((InnerRadius - CameraHeight) / ScaleDepth);
		fCameraOffset = fDepth*fCameraScale;
	}
	else
	{
		// Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
		float B = 2.0 * dot(CameraPos, v3Ray);
		float C = CameraHeight2 - OuterRadius2;
		float fDet = max(0.0, B*B - 4.0 * C);
		float fNear = 0.5 * (-B - sqrt(fDet));

		// Calculate the ray's starting position, then calculate its scattering offset
		v3Start = CameraPos + v3Ray * fNear;
		fFar -= fNear;
		float fDepth = exp((InnerRadius - OuterRadius) / ScaleDepth);
		fCameraOffset = fDepth*fCameraScale;
	}

	// Initialize the scattering loop variables
	float fSampleLength = fFar / Samples;
	float fScaledLength = fSampleLength * ScaleAtmosphere;
	float3 v3SampleRay = v3Ray * fSampleLength;
	float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;

	// Now loop through the sample rays
	float3 v3FrontColor = float3(0.0, 0.0, 0.0);
	float3 v3Attenuate;
	for (int i = 0; i< NumSamples; i++)
	{
		float fHeight = length(v3SamplePoint);
		float fDepth = exp(ScaleOverScaleDepth * (InnerRadius - fHeight));
		float fScatter = fDepth*fTemp - fCameraOffset;
		v3Attenuate = exp(-fScatter * (InvWavelength * Kr4PI + Km4PI));
		v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
		v3SamplePoint += v3SampleRay;
	}

	float3 frontColor =  v3FrontColor * (InvWavelength * KrESun + KmESun);
	float3 secondaryColor = v3Attenuate;

	return float4(frontColor + color*(0.7 + 0.3*secondaryColor), 1);
}