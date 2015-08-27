#include "../MyEffectBase.fxh"

#define NUM_SPLITS 4
#define CASCADE_FAR_BLEND_DISTANCE 300


#define NUM_SPLITS_INV (1.0f / NUM_SPLITS)

uniform float4 ShadowMapSize; //z = 1/x, w = 1/y

float4x4	InvViewMatrix;
float4x4	LightViewProjMatrices [NUM_SPLITS];
float2		ClipPlanes[NUM_SPLITS];
bool		ShowSplitColors = false;
float3		FAR_SHADOW_COLOR = float3(1.0f,1.0f,1.0f);

float ShadowBias = 0.000195f;
float SlopeBias = 0.0195f;
float SlopeCascadeMultiplier = 1;


half3 vSplitColors [4] = 
{
	half3(1, 0, 0),
	half3(0, 1, 0),
	half3(0, 0, 1),
	half3(1, 1, 0)
};


//x = bias multiplier, y = slopebias multiplier
float2 cascadeBiases[4] =  
{
	float2(1.6f, 0.6f), //increasing first one will add space to astronaut feet, scene Quickstart, decreasing will add stripes on slope armor
	float2(1.2f, 0.6f),
	float2(8, 0.6f),  //when lowering check max FOV (90)
	float2(22, 14),	 //when lowering check max FOV (90)
};

float offsets[4] = 
{
	0.0f,
	1.0f / NUM_SPLITS,
	2.0f / NUM_SPLITS,
	3.0f / NUM_SPLITS,
};


texture ShadowMap;
sampler2D ShadowMapSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = POINT; 
    MagFilter = POINT; 
    MipFilter = none; 
};


float CalcShadowTermRndRot(float2 uv,float2 rot,float depth,float bias)
{

	static const float4 filterTaps[4] =
	{
		 {1.0,		 1.0,		0,0},
		 { 1.0 / 2,	-1.0 / 2,	0,0},
		 {-1.0,		-1.0,		0,0},
		 {-1.0 / 2,	 1.0 / 2,	0,0}
	};

	float4 samples;

	samples.x = tex2Dlod(ShadowMapSampler,uv.xyyy + filterTaps[0] * rot.xyyy).x;
	samples.y = tex2Dlod(ShadowMapSampler,uv.xyyy + filterTaps[1] * rot.yxxx).x;
	samples.z = tex2Dlod(ShadowMapSampler,uv.xyyy + filterTaps[2] * rot.xyyy).x;
	samples.w = tex2Dlod(ShadowMapSampler,uv.xyyy + filterTaps[3] * rot.yxxx).x;

	return dot(step(depth,samples + bias),float4(0.25f,0.25f,0.25f,0.25f));
}

// Calculates the shadow occlusion using bilinear PCF
float CalcShadowTermPCF(float fLightDepth, float2 vShadowTexCoord, float bias)
{
	// transform to texel space
	float2 vShadowMapCoord = ShadowMapSize.xy * vShadowTexCoord;
    
	// Determine the lerp amounts           
	float2 vLerps = frac(vShadowMapCoord);

	// Read in the 4 samples, doing a depth check for each
	float4 vShadowTexCoord4 = float4(vShadowTexCoord.x,vShadowTexCoord.y,0,0);

	/*
#ifdef COLOR_SHADOW_MAP_FORMAT
	float sample1 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4));
	float sample2 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(1.0/ShadowMapSize.x, 0)));
	float sample3 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(0, 1.0/ShadowMapSize.y))); 
	float sample4 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(1.0/ShadowMapSize.x, 1.0/ShadowMapSize.y)));
#else
	float sample1 = tex2Dlod(ShadowMapSampler, vShadowTexCoord4).x;
	float sample2 = tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(1.0/ShadowMapSize.x, 0,0,0)).x;
	float sample3 = tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(0, 1.0/ShadowMapSize.y,0,0)).x;
	float sample4 = tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(1.0/ShadowMapSize.x, 1.0/ShadowMapSize.y,0,0)).x;
#endif
*/	

#ifdef COLOR_SHADOW_MAP_FORMAT
	float sample1 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4));
	float sample2 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(ShadowMapSize.z, 0)));
	float sample3 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(0, ShadowMapSize.w))); 
	float sample4 = DecodeFloatRGBA(tex2D(ShadowMapSampler, vShadowTexCoord4 + float2(ShadowMapSize.z, ShadowMapSize.w)));
#else

	float4 sample = float4(
	tex2Dlod(ShadowMapSampler, vShadowTexCoord4).x,
	tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(ShadowMapSize.z, 0,0,0)).x,
	tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(0, ShadowMapSize.w,0,0)).x,
	tex2Dlod(ShadowMapSampler, vShadowTexCoord4 + float4(ShadowMapSize.z, ShadowMapSize.w,0,0)).x
	);
#endif

/*
	fSamples[0] = (sample1 + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[1] = (sample2 + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[2] = (sample3 + bias < fLightDepth) ? 0.0f: 1.0f;  
	fSamples[3] = (sample4 + bias < fLightDepth) ? 0.0f: 1.0f;  
	*/
	/*
	float4 fSamples = float4(
	step(fLightDepth, sample1 + bias),
	step(fLightDepth, sample2 + bias),
	step(fLightDepth, sample3 + bias),
	step(fLightDepth, sample4 + bias));
	  */

	//float4 fSamples = step(fLightDepth, sample + bias);
	//float4 fSamples = (fLightDepth <= sample + bias ? 1 : saturate((1 - sample*3))); //causes bad shadowing in interiors
	float4 fSamples = (fLightDepth <= sample + bias ? 1 : 0);

    
	// lerp between the shadow values to calculate our light amount
	float fShadowTermL = lerp(lerp(fSamples[0], fSamples[1], vLerps.x), lerp( fSamples[2], fSamples[3], vLerps.x), vLerps.y);							  

	return fShadowTermL;								 
}

float GetShadowSample(float2 vSamplePoint, float fLightDepth, float bias)
{
#ifdef COLOR_SHADOW_MAP_FORMAT
			float fDepth = DecodeFloatRGBA(tex2D(ShadowMapSampler, float2(vSamplePoint.x,vSamplePoint.y)));
#else
			float fDepth = tex2Dlod(ShadowMapSampler, float4(vSamplePoint.x,vSamplePoint.y,0,0)).x;
#endif

	//fDepth = pow(2, -10*fDepth);

	//float fSample = (fLightDepth <= fDepth + bias ? 1 : (1 - fDepth));
	float fSample = (fLightDepth <= fDepth + bias ? 1 : 0);
	return fSample;
}

// Calculates the shadow term using PCF soft-shadowing
float CalcShadowTermSoftPCF(float fLightDepth, float2 vShadowTexCoord, int iSqrtSamples, float bias)
{
	float fShadowTerm = 0.0f;  
	float fWeightAccum = 0.0f;
	float centerOffset = -(iSqrtSamples - 1) * 0.5f;
	
	for (int y = 0; y < iSqrtSamples; y++)
	{
		for (int x = 0; x < iSqrtSamples; x++)
		{
			float2 vOffset = float2(x, y) + centerOffset;
			
			// Edge tap smoothing
			float xWeight = 1;
			float yWeight = 1;
			
			// We "hope" that this will be removed by loop unrolling
			if (x == 0)
				xWeight = 1 - frac(vShadowTexCoord.x * ShadowMapSize.x);
			else if (x == iSqrtSamples - 1)
				xWeight = frac(vShadowTexCoord.x * ShadowMapSize.x);
				
			if (y == 0)
				yWeight = 1 - frac(vShadowTexCoord.y * ShadowMapSize.y);
			else if (y == iSqrtSamples - 1)
				yWeight = frac(vShadowTexCoord.y * ShadowMapSize.y);

			vOffset *= ShadowMapSize.zw;
			float2 vSamplePoint = vShadowTexCoord + vOffset;			
			float fSample = GetShadowSample(vSamplePoint, fLightDepth, bias);

			fShadowTerm += fSample * xWeight * yWeight;
			fWeightAccum += xWeight * yWeight;
		}											
	}		
	
	fShadowTerm /= fWeightAccum;
	return fShadowTerm;
}


float3 GetShadowTermFromPosition(float4 vPositionVS, float testDepth, int iFilterSize, float bias, out float diff)
{
	//
	// Trick to efficiently select appropriate cascade http://aras-p.info/blog/2009/11/04/deferred-cascaded-shadow-maps/
	//


	half3 vColor = half3(1,1,1);
	int iCurrentSplit = 0;


				/*
	// Unrolling the loop allows for a performance boost on the 360
	for (int i = 1; i < NUM_SPLITS; i++)
	{
		if (testDepth <= ClipPlanes[i].x && testDepth > ClipPlanes[i].y)
		{
			iCurrentSplit = i;
		}
	}
			  */

	float4 nears = testDepth <= float4(ClipPlanes[0].x,ClipPlanes[1].x,ClipPlanes[2].x,ClipPlanes[3].x);
	float4 fars = testDepth > float4(ClipPlanes[0].y,ClipPlanes[1].y,ClipPlanes[2].y,ClipPlanes[3].y);

	iCurrentSplit = (int)dot(nears * fars, float4(0,1,2,3));
				

	diff = ClipPlanes[iCurrentSplit].y - testDepth;

	if (testDepth <= ClipPlanes[3].y)
	{
			//discard;
			//iCurrentSplit = 2;
			return FAR_SHADOW_COLOR;
	}

	float farBlend = testDepth + CASCADE_FAR_BLEND_DISTANCE <= ClipPlanes[3].y ? (testDepth - ClipPlanes[3].y) / CASCADE_FAR_BLEND_DISTANCE : 1.0f;


	// Figure out which split this pixel belongs to, based on view-space depth.
	float fOffset = offsets[iCurrentSplit];
			
	// If we're showing the split colors, set the coloring
	vColor = ShowSplitColors ? vSplitColors[iCurrentSplit] : vColor;
	
	////////////////////////////////////////////////////
	// Determine the depth of the pixel with respect to the light
	float4x4 matViewToLightViewProj = LightViewProjMatrices[iCurrentSplit];
	//float4x4 matViewToLightViewProj = mul(InvViewMatrix, matLightViewProj);
	
	float4 vPositionLightCS = mul(vPositionVS, matViewToLightViewProj);
	
	vPositionLightCS /= vPositionLightCS.w;

	// Transform from light space to shadow map texture space.
    float2 vShadowTexCoord = 0.5 * vPositionLightCS.xy + float2(0.5f, 0.5f);
    vShadowTexCoord.x = vShadowTexCoord.x * NUM_SPLITS_INV + fOffset;
    vShadowTexCoord.y = 1.0f - vShadowTexCoord.y;
        
    // Offset the coordinate by half a texel so we sample it correctly
    vShadowTexCoord += (0.5f * ShadowMapSize.zw);
	
	////////////////////////////////////////////////////

	// -> predpocitat shadowmap matrices
        

	// Get the shadow occlusion factor and output it
	float fShadowTerm = 0;

	// bias -> pri rendru shadowmapy
	//float biasTotal = ShadowBias * (iCurrentSplit + 1) + SlopeBias * bias * (iCurrentSplit == 0 ? 1 : iCurrentSplit * SlopeCascadeMultiplier);
	float biasTotal = ShadowBias * cascadeBiases[iCurrentSplit].x + SlopeBias * bias * (iCurrentSplit == 0 ? 0.2f : cascadeBiases[iCurrentSplit].y * SlopeCascadeMultiplier);
		  
	if (iFilterSize == 0)
		fShadowTerm = CalcShadowTermPCF(vPositionLightCS.z, vShadowTexCoord,  biasTotal);
	else
		fShadowTerm = CalcShadowTermSoftPCF(vPositionLightCS.z, vShadowTexCoord, iFilterSize, biasTotal);

	//fShadowTerm = GetShadowSample(vShadowTexCoord, vPositionLightCS.z,  biasTotal);

	
	/*
	#if 0
	if (iFilterSize == 2)
		fShadowTerm = CalcShadowTermPCF(fLightDepth, vShadowTexCoord,  biasTotal);
	else
		fShadowTerm = CalcShadowTermSoftPCF(fLightDepth, vShadowTexCoord, iFilterSize, biasTotal);
	#else
		fShadowTerm = CalcShadowTermRndRot(vShadowTexCoord,rndRot,fLightDepth,biasTotal);
	#endif
		*/
	//return fShadowTerm * vColor;	

		//return float3(SlopeBias*abs(ddistddx), SlopeBias*abs(ddistddy), 0);
	return  lerp(FAR_SHADOW_COLOR, fShadowTerm * vColor, farBlend);
}


