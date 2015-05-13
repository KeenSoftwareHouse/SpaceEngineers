//	This shader blends between texture LOD0 and LOD1 according to depth buffer

#include "../MyEffectBase.fxh"

float2 HalfPixel;
float2 Scale;
float LodNear;
float LodFar;
float LodBackgroundStart;
float LodBackgroundEnd;
float4 ColorLayer;
float3 FrustumCorners[4];

//	Texture contains scene from LOD0
Texture Lod0Diffuse;
sampler Lod0DiffuseSampler = sampler_state 
{ 
	texture = <Lod0Diffuse> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains scene from LOD1
Texture Lod1Diffuse;
sampler Lod1DiffuseSampler = sampler_state 
{ 
	texture = <Lod1Diffuse> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture for background
Texture LodBackgroundTexture;
sampler LodBackgroundTextureSampler = sampler_state 
{ 
	texture = <LodBackgroundTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains normals from LOD0
Texture Lod0Normals;
sampler Lod0NormalsSampler = sampler_state 
{ 
	texture = <Lod0Normals> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains normals from LOD1
Texture Lod1Normals;
sampler Lod1NormalsSampler = sampler_state 
{ 
	texture = <Lod1Normals> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains depth from LOD0
Texture Lod0Depth;
sampler Lod0DepthSampler = sampler_state 
{ 
	texture = <Lod0Depth> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains depth from LOD1
Texture Lod1Depth;
sampler Lod1DepthSampler = sampler_state 
{ 
	texture = <Lod1Depth> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
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
	output.TexCoord = (input.TexCoordAndCornerIndex.xy + HalfPixel) * Scale;
	output.ScreenPosition = input.Position;
	output.FrustumCorner = FrustumCorners[input.TexCoordAndCornerIndex.z];
	return output;
}

MyGbufferPixelShaderOutput GetGbufferPixelShaderOutput2(float4 normal, float4 diffuse, float depth)
{
	//	Output into MRT
	MyGbufferPixelShaderOutput output;
	output.NormalAndSpecPower = normal;
	output.DiffuseAndSpecIntensity = diffuse;
	output.DepthAndEmissivity = EncodeFloatRGBA(depth);		
    return output;
}

MyGbufferPixelShaderOutput CalculateBlend(VertexShaderOutput input, int showColorLayer) 
{
	float4 encodedDepth0 = tex2D(Lod0DepthSampler, input.TexCoord);
	float4 encodedDepth1 = tex2D(Lod1DepthSampler, input.TexCoord);

	float depth0 = DecodeFloatRGBA(encodedDepth0);
	float depth1 = DecodeFloatRGBA(encodedDepth1);
		
	float depth0ForTest = GetViewDistanceFromDepth(depth0, input.FrustumCorner);
	float depth1ForTest = GetViewDistanceFromDepth(depth1, input.FrustumCorner);

	float4 diffuse;
	float3 normal = float3(1,0,0);
	float depth = 1;
	float emissive = 0;
	float reflectivity = 0;
	float specularPower = 0;
	float specularIntensity = 0;

	float4 diffuse0 = tex2D(Lod0DiffuseSampler, input.TexCoord);
	float4 encodedNormal0 = tex2D(Lod0NormalsSampler, input.TexCoord);
	float3 normal0 = GetNormalVectorFromRenderTargetNormalized(encodedNormal0.xyz);

	float4 diffuse1 = tex2D(Lod1DiffuseSampler, input.TexCoord);
	float4 encodedNormal1 = tex2D(Lod1NormalsSampler, input.TexCoord);
	float3 normal1 = GetNormalVectorFromRenderTargetNormalized(encodedNormal1.xyz);
	
	float4 diffuseBackground = tex2D(LodBackgroundTextureSampler, input.TexCoord);
	
	diffuse0 = IsDepthBackground(depth0) ? float4(diffuseBackground.xyz, 0) : diffuse0;
	diffuse1 = IsDepthBackground(depth1) ? float4(diffuseBackground.xyz, 0) : diffuse1;

	float depthForTest = depth0ForTest <= depth1ForTest ? depth0ForTest : depth1ForTest;
	float backgroundBlend = 0;

	if (depth1ForTest > LodFar)
	{
		if (depth1ForTest < LodBackgroundStart)
		{
			if (showColorLayer)
				diffuse1 = float4(lerp(diffuse1.rgb, ColorLayer.rgb, ColorLayer.a), diffuse1.a);
		}
		else if (depth1ForTest > LodBackgroundEnd)
		{
			diffuse1 = diffuseBackground;
			diffuse1.w = 0; //disable specular on background
			depth1 = 0.99999f;
			normal1 = 0;
	//		backgroundBlend = 1;
//			diffuse1 = float4(0,0,1,1);
//			diffuse1 = diffuseBackground;
		}
		else
		{
			if (showColorLayer)
				diffuse1 = float4(lerp(diffuse1.rgb, ColorLayer.rgb, ColorLayer.a), diffuse1.a);

			float blend = (depth1ForTest - LodBackgroundStart) / (LodBackgroundEnd - LodBackgroundStart);
			diffuse1 = lerp(diffuse1, float4(diffuseBackground.xyz, 0), blend);
			normal1 *= (1 - blend);
			//diffuse1 = float4(0,0,1,1);
		}
		//diffuse1 = float4(0,0,1,1);
	}
	else
	if (depth1ForTest < LodNear)
	{   //tunnel in the ring + cut in the station asteroid
		//vs. borders at asteroid field
		if (!IsDepthBackground(depth0))
		{   //Must not happen now
			diffuse1 = float4(0,1,0,1);
			diffuse1.w = 0;
			normal1 = 0;
			depth1 = 0.99999f;
		}
	}
	
	if (IsDepthBackground(depth0) && (depthForTest == depth1ForTest))
	{	//solves transparent stripes at full asteroids sector 
		diffuse0 = diffuse1;
		normal0 = normal1;
		depth0 = depth1;
	}


	if (depthForTest < LodNear)
	{
		if (IsDepthBackground(depth0) && (depthForTest == depth1ForTest))
		{ //we have empty LOD0 and another LOD1 behind
			diffuse = diffuse1;
			normal = normal1;
			depth = depth1;
			emissive = UnpackGBufferEmissivity(encodedDepth1.w);
			reflectivity = UnpackGBufferReflection(encodedDepth1.w);
			specularPower = encodedNormal1.w;
			specularIntensity = diffuse1.w;
		}	
		else
		{
			diffuse = diffuse0;
			normal = normal0;
			depth = depth0;
			emissive = UnpackGBufferEmissivity(encodedDepth0.w);
			reflectivity = UnpackGBufferReflection(encodedDepth0.w);
			specularPower = encodedNormal0.w;
			specularIntensity = diffuse0.w;

		//	diffuse = float4(0,1,0,1);
		//	normal = float3(0,0,1);

		}		
		
		//diffuse = float4(1,1,0,1);	
		//normal = float3(0,0,1);
	}	
	else
	if (depthForTest > LodFar)
	{
		normal = normal1;
		depth = depth1;
		emissive = UnpackGBufferEmissivity(encodedDepth1.w);
		reflectivity = UnpackGBufferReflection(encodedDepth1.w);
		specularPower = encodedNormal1.w;
		specularIntensity = diffuse1.w;

		//Because in LOD1 can be totally different object, which can be blended into background
		//depthForTest = depth1ForTest;
	  	diffuse = float4(1,1,1,1);

		if (depthForTest < LodBackgroundStart)
		{
			if (showColorLayer)
				diffuse1 = float4(lerp(diffuse1.rgb, ColorLayer.rgb, ColorLayer.a), diffuse1.a);

			diffuse = diffuse1;
		}
		else
		if (depthForTest > LodBackgroundEnd)
		{
		
			diffuse = diffuseBackground;
			diffuse.w = 0; //disable specular on background
			//diffuseBackground.w = 0; //disable specular on background
			//diffuse = diffuseBackground;
			depth = 0.99999f;
			normal = 0;
			backgroundBlend = 1;
		
		}
		else	
		{							   
			if (showColorLayer)
				diffuse1 = float4(lerp(diffuse1.rgb, ColorLayer.rgb, ColorLayer.a), diffuse1.a);

			backgroundBlend = (depthForTest - LodBackgroundStart) / (LodBackgroundEnd - LodBackgroundStart);
			diffuse = lerp(diffuse1, float4(diffuseBackground.xyz, 0), backgroundBlend);
			normal *= (1 - backgroundBlend);	 

			/*
			if (depthForTest < LodBackgroundStart)
			{
				 diffuse = float4(0,1,1,1);
			} */
			//diffuse = diffuse1;
		}

		//diffuse = float4(0,1,1,1);	
		//normal = float3(0,0,1);
	}
	else
	{
		if (showColorLayer)
			diffuse1 = float4(lerp(diffuse1.rgb, ColorLayer.rgb, ColorLayer.a), diffuse1.a);

		float blend = (depthForTest - LodNear) / (LodFar - LodNear);
		diffuse = lerp(diffuse0, diffuse1, blend);
		specularPower = encodedNormal0.w;
		specularIntensity = diffuse0.w;

		if (IsDepthBackground(depth1))
		{
			depth = depth0;
			normal = normal0;
			normal *= (1 - blend); //blend to background
			backgroundBlend = blend;
		}
		else
		{

			if (IsDepthBackground(depth0))
			{ //we have LOD1 bigger than LOD0 and something is behind us
				normal = normal1;
				depth = depth1;
			}
			else
			{
				//depth =  depth0; //looks better than lerp
				//depth =  min(depth0, depth1);

				//If not used abs here, then false shadows/lighting is on blended LOD0/1 with large z difference. In this case
				//we want to use closer object lighting, not interpolated
				if (abs(depth1ForTest - depth0ForTest) > 20)
				{
					depth = min(depth0, depth1);
				}
				else 
				{
					depth = lerp(depth0, depth1, blend);
					//depth = min(depth0, depth1);
				}
				normal = lerp(normal0, normal1, blend);
				diffuse = lerp(diffuse0, diffuse1, blend);
				//diffuse = float4(1,1,0,1);
			}

			//normal = normal0;
			//With normalize causes sharp transition inside very lowpoly ring
			//With normalize causes also black borders at big asteroids on LOD0/1 transitions
			//Without normalize causes transluent blend with background on LOD0/1
			normal = normalize(normal); 

			//diffuse = float4(1,0,1,1);	
			//normal = float3(0,0,1);
		}
		
//diffuse = diffuse1;
		float emissive0 = UnpackGBufferEmissivity(encodedDepth0.w);
		float emissive1 = UnpackGBufferEmissivity(encodedDepth1.w);
		emissive = lerp(emissive0, emissive1, blend);


		if (depth0ForTest <= depth1ForTest)
		{
			//depth = depth0;
			reflectivity = UnpackGBufferReflection(encodedDepth0.w);
		}
		else
		{
			//depth = depth1;
			reflectivity = UnpackGBufferReflection(encodedDepth1.w);
		}
	}

	float4 encodedNormal;
	encodedNormal.xyz = GetNormalVectorIntoRenderTarget(normal);
	encodedNormal.w = specularPower;

	diffuse.w = specularIntensity;

	//	Output into MRT
	MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutput2(encodedNormal, diffuse, depth);
	//output.Diffuse.rgb = float3( 1 - emissive,1 - emissive,1 - emissive);

	if (backgroundBlend > 0)
	{
		emissive += FogBacklightMultiplier * diffuseBackground.w * backgroundBlend;
	}

	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection(emissive, reflectivity);
	
	return output;
}

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	return CalculateBlend(input, 0);
}


MyGbufferPixelShaderOutput PixelShaderFunctionColorLayer(VertexShaderOutput input)
{
	return CalculateBlend(input, 1);
}


technique BasicTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}

technique ColorLayerTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunctionColorLayer();
	}
}