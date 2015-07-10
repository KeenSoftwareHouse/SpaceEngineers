#include "../MyEffectBase.fxh"

//source here http://www.gamedev.net/topic/495974-deconstructing-crysis-ssao-shader/

Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture RandomTexture;
sampler RandomTextureSampler = sampler_state 
{ 
	texture = <RandomTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

float2 HalfPixel;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float4 ScreenPosition : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoord + HalfPixel;
	output.ScreenPosition = input.Position;
	return output;
}

float4x4 ViewMatrix;

// g_SSAOParams.x	= radius
// g_SSAOParams.y = fallof
// g_SSAOParams.z = zf
// g_SSAOParams.w = occlusion samples normalization value * color scale
uniform float4	g_SSAOParams = { 1.0f, 0.0f, 35000, 1.0f};

// g_SSAOParams2.x = bias
// g_SSAOParams2.y = zscale
// g_SSAOParams2.z = color bias

float scaleMin = 13.000f;
float scaleMax = 3.486f;

float depthSoft = 9.457f;	 //0.852
float zScale = 0.551f;	 //0.105


uniform float4	g_SSAOParams2 = {0.715, 0, 0.337f, 1};

float3 mirror( float3 vDir, float3 vPlane ) { return vDir - 2 * vPlane * dot(vPlane,vDir);}

float4 SSAO(float2 uv,float2 screenPos,const uniform bool highQuality)
{

	// define kernel
	const half step	= 1.f - 1.f/8.f;
	half	n				= 0;
	const half fScale = 0.025f / 3; 
	const half3 arrKernel[8] =
	{
		normalize(half3( 1, 1, 1))*fScale*(n+=step),
		normalize(half3(-1,-1,-1))*fScale*(n+=step),
		normalize(half3(-1,-1, 1))*fScale*(n+=step),
		normalize(half3(-1, 1,-1))*fScale*(n+=step),
		normalize(half3(-1, 1 ,1))*fScale*(n+=step),
		normalize(half3( 1,-1,-1))*fScale*(n+=step),
		normalize(half3( 1,-1, 1))*fScale*(n+=step),
		normalize(half3( 1, 1,-1))*fScale*(n+=step),
	};

	half		res;
	float		fSceneDepth		= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv));

	if (fSceneDepth < 1)
	{
		float4	SSAO_params		= g_SSAOParams2;
		float		far				= g_SSAOParams.z;
		half3		rotSample		= (2 * tex2D(RandomTextureSampler,float2(0,0).xyyy).rgb - 1);
		float		fSceneDepthM	= fSceneDepth * far;  
		float sm = scaleMax;

		if (fSceneDepthM < 7)
		{
			sm = 20;
		}

		// make area smaller if distance less than X meters + bigger if distance more than 32 meters
		float3	vSampleScale		= SSAO_params.zzw * saturate(fSceneDepthM / scaleMin) * (1.f + fSceneDepthM / sm);
		float		fDepthRangeScale	= far / vSampleScale.z * 0.85f;
	
		// convert from meters into SS units
		vSampleScale.xy *= 1.0f / fSceneDepthM;
		vSampleScale.z  *= zScale / far;

		float fDepthTestSoftness = 2 * depthSoft/vSampleScale.z;

		half4		vSkyAccess = 0.f;
		half4		arrSceneDepth2[2];
		half3		vIrrSample;
		half4		vDistance;
		float4		fRangeIsInvalid;
		float		fHQScale = 0.85f;

		for (int i = 0; i < 2; i++)
		{
			vIrrSample					= mirror(arrKernel[i * 4 + 0], rotSample) * vSampleScale;
			arrSceneDepth2[0].x		= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;

			if (highQuality)
			{
				vIrrSample.xyz			*= fHQScale;
				arrSceneDepth2[1].x	= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;
			}

			vIrrSample					= mirror(arrKernel[i * 4 + 1], rotSample) * vSampleScale;
			arrSceneDepth2[0].y		= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;

			if (highQuality)
			{
				vIrrSample.xyz			*= fHQScale;
				arrSceneDepth2[1].y	=	DecodeFloatRGBA(tex2D(DepthsRTSampler, uv.xy + vIrrSample.xy)) + vIrrSample.z;
			}

			vIrrSample					= mirror(arrKernel[i * 4 + 2], rotSample) * vSampleScale;
			arrSceneDepth2[0].z		= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;
			
			if (highQuality)
			{
				vIrrSample.xyz			*= fHQScale;
				arrSceneDepth2[1].z	=	DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;
			}

			vIrrSample					= mirror(arrKernel[i * 4 + 3], rotSample) * vSampleScale;
			arrSceneDepth2[0].w		= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;
			
			if (highQuality)
			{
				vIrrSample.xyz			*= fHQScale;
				arrSceneDepth2[1].w	= DecodeFloatRGBA(tex2D(DepthsRTSampler,uv.xy + vIrrSample.xy)) + vIrrSample.z;
			}

			float fDefVal = g_SSAOParams.y;			

			for(int s = 0; s < (highQuality ? 2 : 1); s++)
			{
				vDistance = fSceneDepth - arrSceneDepth2[s];
				float4 vDistanceScaled = vDistance * fDepthRangeScale;
				fRangeIsInvalid = (saturate( abs(vDistanceScaled) ) + saturate( vDistanceScaled ))/2;
				vSkyAccess += lerp(saturate((-vDistance)*fDepthTestSoftness), fDefVal, fRangeIsInvalid);
			}
		}

		//original SSAO
		res = dot( vSkyAccess, (highQuality ? 1/16.0f : 1/8.0f)*2.0 ) - SSAO_params.y * smoothstep(7, 10, fSceneDepthM);

		//Bias
		res = saturate(lerp( 0.9f, res, SSAO_params.x ));

		//Contrast
		res = (1 - res)*(1 - res);

		//Bias by distance
		res = fSceneDepthM < 7 ? res : min(res,(fSceneDepth + 0.001f) * 300.0f); 

		//Clamp to 0,1
		res = saturate(res);

		return float4(0,0,0, 1 - res);
	}
	else
	{
		return 1;
	}
};




float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	return SSAO(input.TexCoord, input.ScreenPosition, false);
}







technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}
