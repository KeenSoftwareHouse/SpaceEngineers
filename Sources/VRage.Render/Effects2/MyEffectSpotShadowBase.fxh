#include "../MyEffectBase.fxh"

float		ShadowBias = 0.000195f;
float		SlopeBias = 0.0195f;
float2		ShadowMapSize;
float4x4	InvViewMatrix;
float4x4	LightViewProjectionShadow;

texture ShadowMap;
sampler2D ShadowMapSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = POINT; 
    MagFilter = POINT; 
    MipFilter = none; 
};

// Calculates the shadow occlusion using bilinear PCF
float CalcShadowTermPCF(float fLightDepth, float2 vShadowTexCoord, float bias)
{
	// transform to texel space
	float2 vShadowMapCoord = ShadowMapSize * vShadowTexCoord;
    
	// Determine the lerp amounts           
	float2 vLerps = frac(vShadowMapCoord);

	// Read in the 4 samples, doing a depth check for each
	float fSamples[4];	
	float4 vShadowTexCoord4 = float4(vShadowTexCoord.x,vShadowTexCoord.y,0,0);
	fSamples[0] = (tex2Dlod(ShadowMapSampler, vShadowTexCoord4).x + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[1] = (tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(1.0/ShadowMapSize.x, 0,0,0)).x + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[2] = (tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(0, 1.0/ShadowMapSize.y,0,0)).x + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[3] = (tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(1.0/ShadowMapSize.x, 1.0/ShadowMapSize.y,0,0)).x + bias < fLightDepth) ? 0.0f: 1.0f;  
    
	// lerp between the shadow values to calculate our light amount
	float fShadowTermL = lerp(lerp(fSamples[0], fSamples[1], vLerps.x), lerp( fSamples[2], fSamples[3], vLerps.x), vLerps.y);							  

	return fShadowTermL;								 
}


float GetShadowSample(float2 vSamplePoint, float fLightDepth, float bias)
{
	float fDepth = tex2Dlod(ShadowMapSampler, float4(vSamplePoint.x,vSamplePoint.y,0,0)).x;

	//fDepth = pow(2, -10*fDepth);

	float fSample = (fLightDepth <= fDepth + bias);
	return fSample;
}

// Calculates the shadow term using PCF soft-shadowing
float CalcShadowTermSoftPCF(float fLightDepth, float2 vShadowTexCoord, int iSqrtSamples, float bias)
{
	float fShadowTerm = 0.0f;  
		
	float fRadius = (iSqrtSamples - 1.0f) / 2;
	float fWeightAccum = 0.0f;
	
	for (float y = -fRadius; y <= fRadius; y++)
	{
		for (float x = -fRadius; x <= fRadius; x++)
		{
			float2 vOffset = 0;
			vOffset = float2(x, y);				
			vOffset /= ShadowMapSize;
			float2 vSamplePoint = vShadowTexCoord + vOffset;			
			float fSample = GetShadowSample(vSamplePoint, fLightDepth, bias);
			
			// Edge tap smoothing
			float xWeight = 1;
			float yWeight = 1;
			
			if (x == -fRadius)
				xWeight = 1 - frac(vShadowTexCoord.x * ShadowMapSize.x);
			else if (x == fRadius)
				xWeight = frac(vShadowTexCoord.x * ShadowMapSize.x);
				
			if (y == -fRadius)
				yWeight = 1 - frac(vShadowTexCoord.y * ShadowMapSize.y);
			else if (y == fRadius)
				yWeight = frac(vShadowTexCoord.y * ShadowMapSize.y);
				
			fShadowTerm += fSample * xWeight * yWeight;
			fWeightAccum = xWeight * yWeight;
		}											
	}		
	
	fShadowTerm /= (iSqrtSamples * iSqrtSamples);
	fShadowTerm *= 1.55f;	
	
	return fShadowTerm;
}


float3 GetShadowTermFromPosition(float4 vPositionVS, float testDepth, uniform int iFilterSize, float bias)
{
	// Determine the depth of the pixel with respect to the light
	float4x4 matViewToLightViewProj = mul(InvViewMatrix, LightViewProjectionShadow);
	
	float4 vPositionLightCS = mul(vPositionVS, matViewToLightViewProj);
	
	half fLightDepth = vPositionLightCS.z / vPositionLightCS.w;	
	
	// Transform from light space to shadow map texture space.
    half2 vShadowTexCoord = 0.5 * vPositionLightCS.xy / vPositionLightCS.w + float2(0.5f, 0.5f);
    vShadowTexCoord.x = vShadowTexCoord.x;
    vShadowTexCoord.y = 1.0f - vShadowTexCoord.y;
        
    // Offset the coordinate by half a texel so we sample it correctly
    vShadowTexCoord += (0.5f / ShadowMapSize);
	
	float fShadowTerm = 0;

	if (iFilterSize > 0)
	{
		half biasTotal = ShadowBias + SlopeBias * bias * bias * bias;

		// Get the shadow occlusion factor and output it
		fShadowTerm = CalcShadowTermSoftPCF(fLightDepth, vShadowTexCoord, iFilterSize, biasTotal);
	}
	else
	{
		fShadowTerm = GetShadowSample(vShadowTexCoord, fLightDepth, 0);
	}
		
	return fShadowTerm;	

		//return float3(SlopeBias*abs(ddistddx), SlopeBias*abs(ddistddy), 0);
	//return  lerp(FAR_SHADOW_COLOR, fShadowTerm * vColor, farBlend);
}