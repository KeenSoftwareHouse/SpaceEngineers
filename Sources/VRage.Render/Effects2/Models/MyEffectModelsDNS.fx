
#include "../MyEffectDynamicLightingBase.fxh"
#include "../MyEffectReflectorBase.fxh"
#include "../MyEffectInstanceBase.fxh"
 
//	This shader renders a model with diffuse & specular & normal map textures, so it requires certain vertex shader data

float4x4	WorldMatrix; 
float4x4	ViewMatrix;
float4x4	ProjectionMatrix;
float3	    DiffuseColor; 

float4x4 Bones[SKINNED_EFFECT_MAX_BONES];

float	    Emissivity = 0; 
float	    EmissivityOffset = 0; 
float2	    EmissivityUVAnim; 
float2	    DiffuseUVAnim; 
float3	    Highlight = 0; 

float		SpecularIntensity = 1;
float		SpecularPower = 1;

float		Dithering = 0;
float2		TextureDitheringSize;

//Needed for randomized effects (jitter, noise, etc)
//Not the actual time, just TickCount
float		Time;

float2 HalfPixel;
float2 Scale;

Texture TextureDiffuse;
sampler TextureDiffuseSampler = sampler_state 
{ 
	texture = <TextureDiffuse>; 
	mipfilter = LINEAR;
	AddressU = WRAP; 
	AddressV = WRAP;
};

Texture TextureNormal;
sampler TextureNormalSampler = sampler_state 
{ 
	texture = <TextureNormal> ; 
	mipfilter = LINEAR; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

Texture TextureDithering;
sampler TextureDitheringSampler = sampler_state 
{ 
	texture = <TextureDithering> ; 
	mipfilter = NONE; 
	magfilter = POINT; 
	minfilter = POINT;
	AddressU = WRAP; 
	AddressV = WRAP;
};

//This sampler is used for HOLO objects
Texture DepthTextureNear;
sampler DepthTextureNearSampler = sampler_state 
{ 
	texture = <DepthTextureNear>; 
	magfilter = POINT; 
	minfilter = POINT;
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

//This sampler is used for HOLO objects
Texture DepthTextureFar;
sampler DepthTextureFarSampler = sampler_state 
{ 
	texture = <DepthTextureFar>; 
	magfilter = POINT; 
	minfilter = POINT;
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

struct VertexShaderOutput_DNS
{
	VertexShaderOutputLow_DNS BaseOutput;
	float3x3 TangentToWorld : TEXCOORD5;
};
//DNS Skinned (7 input registers)
struct VertexShaderInputSkinned_DNS
{
    VertexShaderInput_DNS TangentInput;
    int4 Indices:  BLENDINDICES0;
    float4 Weights  : BLENDWEIGHT0;
};



// Low VS

VertexShaderOutputLow_DNS VertexShaderFunctionLow_DNS_Base(VertexShaderInputLow_DNS input, float4x4 world)
{
	VertexShaderOutputLow_DNS output;

	input.Position = UnpackPositionAndScale(input.Position);
	input.Normal = UnpackNormal(input.Normal);

	output = (VertexShaderOutputLow_DNS)0;
	output.WorldPos = input.Position;
	output.Position = mul(input.Position, world);
    output.Position = mul(output.Position, ViewMatrix);
	output.TexCoordAndViewDistance.z = -output.Position.z;
	output.TexCoordAndViewDistance.w = length(output.Position.xyz);
    output.Position = mul(output.Position, ProjectionMatrix);    
	output.ScreenPosition = output.Position;
    output.TexCoordAndViewDistance.xy = input.TexCoord;
	output.Normal =  normalize(mul(input.Normal.xyz, (float3x3)world));    
	output.Color = float4(ColorMaskEnabled * ColorMaskHSV, Dithering);

    return output;
}

VertexShaderOutputLow_DNS VertexShaderFunctionLow_DNS(VertexShaderInputLow_DNS input)
{
    return VertexShaderFunctionLow_DNS_Base(input, WorldMatrix);
}

// Normal, High, Extreme VS

VertexShaderOutput_DNS VertexShaderFunction_DNS_Base(VertexShaderInput_DNS input, float4x4 world)
{
	VertexShaderOutput_DNS output;

	output.BaseOutput = VertexShaderFunctionLow_DNS_Base(input.BaseInput, world);
	
	input.Tangent = UnpackTangent(input.Tangent);
	input.BaseInput.Normal = UnpackNormal(input.BaseInput.Normal);

    output.TangentToWorld[0] = mul(input.Tangent.xyz, (float3x3)world);
    output.TangentToWorld[2] = output.BaseOutput.Normal;
	output.TangentToWorld[1] = cross(output.TangentToWorld[0], output.BaseOutput.Normal) * input.Tangent.w;

    return output;
}

VertexShaderOutput_DNS VertexShaderFunction_DNS(VertexShaderInput_DNS input)
{
    return VertexShaderFunction_DNS_Base(input, WorldMatrix);
}

VertexShaderOutput_DNS VertexShaderFunctionSkinned_DNS(VertexShaderInputSkinned_DNS input)
{
	float4x4 world = 0;
	
    for (int i = 0; i < 4; i++) 
    {
        world += Bones[input.Indices[i]] * input.Weights[i];
    }

	return VertexShaderFunction_DNS_Base(input.TangentInput, mul(world, WorldMatrix));
}

VertexShaderOutput_DNS VertexShaderFunctionInstancedSkinned_DNS(VertexShaderInputSkinned_DNS input, VertexShaderInput_InstanceData instanceData)
{
	VertexShaderOutput_DNS output;
	float4 maskHSV;
	float4x4 instanceMatrix = GetInstanceMatrix(input.TangentInput.BaseInput.Position, input.Indices, input.Weights, instanceData, maskHSV);
	output = VertexShaderFunction_DNS_Base(input.TangentInput, mul(instanceMatrix, WorldMatrix));

	output.BaseOutput.TexCoordAndViewDistance.xy += float2(instanceData.bones6.w, instanceData.bones7.w);
	
	output.BaseOutput.Color.xyz = maskHSV.xyz;
	output.BaseOutput.Color.w = max(output.BaseOutput.Color.w, abs(maskHSV.w)); // Higher of dithering values is used
	if (sign(maskHSV.w) < 0) // Keep sign
	{
		output.BaseOutput.Color.w = -output.BaseOutput.Color.w;
	}
	return output;
}

VertexShaderOutput_DNS VertexShaderFunctionInstanced_DNS(VertexShaderInput_DNS input, VertexShaderInput_InstanceData instanceData)
{
	VertexShaderOutput_DNS output;
	float4 maskHSV;
	float4x4 instanceMatrix = GetInstanceMatrixOnlyPosition(input.BaseInput.Position, instanceData, maskHSV);
	output = VertexShaderFunction_DNS_Base(input, mul(instanceMatrix, WorldMatrix));

	output.BaseOutput.TexCoordAndViewDistance.xy += float2(instanceData.bones6.w, instanceData.bones7.w);
	
	output.BaseOutput.Color.xyz = maskHSV.xyz;
	output.BaseOutput.Color.w = max(output.BaseOutput.Color.w, abs(maskHSV.w)); // Higher of dithering values is used
	if (sign(maskHSV.w) < 0) // Keep sign
	{
		output.BaseOutput.Color.w = -output.BaseOutput.Color.w;
	}
	return output;
}

VertexShaderOutput_DNS VertexShaderFunctionInstancedGeneric_DNS(VertexShaderInput_DNS input, VertexShaderInput_GenericInstanceData instanceData)
{
	VertexShaderOutput_DNS output;
	
	matrix instanceMatrix = matrix(instanceData.matrix_row0, instanceData.matrix_row1, instanceData.matrix_row2, float4(0,0,0,1));
	instanceMatrix = transpose(instanceMatrix);

	output = VertexShaderFunction_DNS_Base(input, mul(instanceMatrix, WorldMatrix));
	output.BaseOutput.Color.xyz = instanceData.colorMaskHSV.xyz;
	output.BaseOutput.Color.w = max(output.BaseOutput.Color.w, abs(instanceData.colorMaskHSV.w)); // Higher of dithering values is used
	if (sign(instanceData.colorMaskHSV.w) < 0) // Keep sign
	{
		output.BaseOutput.Color.w = -output.BaseOutput.Color.w;
	}

	return output;
}

MyGbufferPixelShaderOutput CalculateOutput(VertexShaderOutputLow_DNS input, float3 normal, float specularIntensity, float3 diffuseColor, float3 si_sp_e, float3 highlight, float emit = 1)
{
	//To check normals from vertices
	//normal.xyz = normalize(input.TangentToWorld[2]);    
	//float3 diffusec = GetNormalVectorIntoRenderTarget(normalize(input.TangentToWorld[1]));

	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.TexCoordAndViewDistance.xy);
	if(ColorMaskEnabled > 0)
	{
		diffuseTexture = ColorizeTexture(diffuseTexture,input.Color.xyz);
	}

	float3 diffuse = diffuseTexture.xyz * diffuseColor.xyz;// + input.Color;
	//float fogBlend = (input.TexCoordAndViewDistance.z - FogDistanceNear) / (FogDistanceFar - FogDistanceNear);
	//diffuse = lerp(diffuse, FogColor, saturate(fogBlend) * FogMultiplier);
	
	// Use ordered dithering, store numbers in constant buffer, it won't require texture anymore
	// http://en.wikipedia.org/wiki/Ordered_dithering

	//Hologram
	if(input.Color.w < 0)
	{
		float2 pixelCount = 1 / (2 * HalfPixel);
		float2 tex = input.ScreenPosition.xy / input.ScreenPosition.w + HalfPixel;
		float2 p = tex / 2 + 1;

		float dither = tex2D(TextureDitheringSampler, p * pixelCount / TextureDitheringSize).x;

		// TODO: Clip would be better?
		if(dither < -input.Color.w)
		{
			discard;
		}
		else
		{
			float t = Time / 10000.0;
			float2 screenPos = input.ScreenPosition.xy / input.ScreenPosition.w + HalfPixel;
			float2 param = float2(t, screenPos.x + screenPos.y);
			float flicker = frac(sin(dot(param, float2(12.9898,78.233))) * 43758.5453) * 0.2 + 0.8;

			float offset = t * 500.0 * 0.2 + frac(sin(dot(screenPos.x, float2(12.9898,78.233))) * 43758.5453) * 1.5;
			float3 overlay = tex2D(TextureDitheringSampler, screenPos.yy * 16.0 + offset / 8.0).rgb;
			
			diffuse *= flicker * pow(overlay, 1.5);

			if (input.Color.w >= -0.25)
			{
				diffuse *= 1.5;
			}
			si_sp_e.z = 1;
		}
	}
	else if(input.Color.w > 0)
	{
		float2 pixelCount = 1 / (2 * HalfPixel);
		float2 tex = input.ScreenPosition.xy / input.ScreenPosition.w + HalfPixel;
		float2 p = tex / 2 + 1;

		float dither = tex2D(TextureDitheringSampler, p * pixelCount / TextureDitheringSize).x;
		
		// TODO: Clip would be better?
		if(dither < input.Color.w)
		{
			discard;
		}
		else
		{
			float mult = 1.5f;
			if (length(diffuse) < 0.3f)
				diffuse = max(diffuse, float3(0.4f,0.4f,0.4f));

			diffuse = ((diffuse - 0.5f) * max(mult, 0)) + 0.5f;
			diffuse = diffuse - mult * (diffuse - 1.0f) * diffuse*(diffuse - 0.5f);
			

			float flatlight = max(abs(dot(normal, normalize(float3(2,3,4)))), 0.3f);
			diffuse *= flatlight;
			si_sp_e.z = 1;
		}
	}

	//	Output into MRT
	MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutput(normal.xyz,  diffuse + highlight, 
	specularIntensity * si_sp_e.x / SPECULAR_INTENSITY_RATIO, si_sp_e.y / SPECULAR_POWER_RATIO, input.TexCoordAndViewDistance.z);

	//inverted emissivity, reflection by specular intensity
	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection(emit * ((1 - diffuseTexture.w) + (si_sp_e.z + length(highlight))), 1.0f);
	return output;
}

// Low PS

MyGbufferPixelShaderOutput PixelShaderFunctionLow_DNS_Base(VertexShaderOutputLow_DNS input, float3 diffuse, float3 si_sp_e, float3 highlight, float emit = 1)
{
	return CalculateOutput(input, input.Normal, 1, diffuse, si_sp_e, highlight, emit);
}

MyGbufferPixelShaderOutput PixelShaderFunctionLow_DNS(VertexShaderOutputLow_DNS input)
{
    return PixelShaderFunctionLow_DNS_Base(input, DiffuseColor, float3(SpecularIntensity, SpecularPower, Emissivity), Highlight);
}


// Normal, High, Extreme PS

MyGbufferPixelShaderOutput PixelShaderFunction_DNS_Base(VertexShaderOutput_DNS input, float3 diffuse, float3 si_sp_e, float3 highlight, float emit = 1)
{
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.BaseOutput.TexCoordAndViewDistance.xy);

	input.TangentToWorld[0] = normalize(input.TangentToWorld[0]);
	input.TangentToWorld[1] = normalize(input.TangentToWorld[1]);
	input.TangentToWorld[2] = normalize(input.TangentToWorld[2]);
    
	float4 encodedNormal = tex2D(TextureNormalSampler, input.BaseOutput.TexCoordAndViewDistance.xy);
    float3 normal = GetNormalVectorFromDDS(encodedNormal);
    normal.xyz = normalize(mul(normal.xyz, input.TangentToWorld));    

	//float specularIntensity = encodedNormal.x; //swizzled x and w
	float specularIntensity = encodedNormal.w; //non-swizzled x and w
	
	return CalculateOutput(input.BaseOutput, normal, specularIntensity, diffuse, si_sp_e, highlight, emit);
}

MyGbufferPixelShaderOutput PixelShaderFunction_DNS(VertexShaderOutput_DNS input)
{
    //Cut pixels from LOD1 which are before LodNear
	/*if (input.BaseOutput.TexCoordAndViewDistance.w < LodCut)
	{
		discard;
		return (MyGbufferPixelShaderOutput)0;
		//return PixelShaderFunction_Base(input, float4(1,0,0,1), Highlight, float3(SpecularIntensity, SpecularPower, 0), renderQuality);
	}
	else*/
	/*if (IsPixelCut(input.BaseOutput.TexCoordAndViewDistance.w))
	{
		discard; 
		return (MyGbufferPixelShaderOutput)0;
	}
	else*/
	{
		return PixelShaderFunction_DNS_Base(input, DiffuseColor, float3(SpecularIntensity, SpecularPower, Emissivity), Highlight);
	}
}

MyGbufferPixelShaderOutput CalculateValuesBlended(VertexShaderOutputLow_DNS input, float4 normal)
{
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.TexCoordAndViewDistance.xy);
  
	float4 diffuseColor = float4(diffuseTexture.xyz * DiffuseColor.xyz + Highlight, diffuseTexture.a);

	float emissivity = (1 - normal.w) + (Emissivity + length(Highlight));

	//diffuseColor = float4(1,1,0,1);

	//	Output into MRT
	MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutputBlended(float4(normal.xyz, diffuseColor.a), diffuseColor, emissivity, 1.0f);	
	output.DepthAndEmissivity.a = PackGBufferEmissivityReflection(emissivity, 1.0f);
	return output;
}

MyGbufferPixelShaderOutput PixelShaderFunctionLow_DNS_Blended(VertexShaderOutputLow_DNS input)
{
	float4 normal = GetNormalVectorFromRenderTarget(tex2D(TextureNormalSampler, input.TexCoordAndViewDistance.xy));
	normal.xyz = input.Normal;
	normal.w = 0;
	return CalculateValuesBlended(input, normal);
}


MyGbufferPixelShaderOutput PixelShaderFunction_DNS_Blended(VertexShaderOutput_DNS input)
{
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.BaseOutput.TexCoordAndViewDistance.xy);

	input.TangentToWorld[0] = normalize(input.TangentToWorld[0]);
	input.TangentToWorld[1] = normalize(input.TangentToWorld[1]);
	input.TangentToWorld[2] = normalize(input.TangentToWorld[2]);
    
    float4 normal = GetNormalVectorFromRenderTarget(tex2D(TextureNormalSampler, input.BaseOutput.TexCoordAndViewDistance.xy));
    normal.xyz = normalize(mul(normal.xyz, input.TangentToWorld));    
    
	return CalculateValuesBlended(input.BaseOutput, normal);
}

MyGbufferPixelShaderOutput CalculateOutputHolo(VertexShaderOutputLow_DNS input, float4 normal)
{	
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.TexCoordAndViewDistance.xy + EmissivityOffset * DiffuseUVAnim);
    float emissivity2 = 1 - tex2D(TextureNormalSampler, input.TexCoordAndViewDistance.xy + EmissivityOffset * EmissivityUVAnim).w;
	
	float4 diffuseColor = float4(diffuseTexture.xyz * DiffuseColor.xyz + Highlight, diffuseTexture.a);

	//diffuseColor.xyz = float3(1,0,0);
					 
	float emissivity = emissivity2 * diffuseTexture.a + (length(Highlight));
	
	//diffuseColor = float4(1,0,1,1);

	//	Output into MRT
	MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutputBlended(float4(normal.xyz, diffuseColor.a), diffuseColor, emissivity, 1.0f);
	return output; 
}


MyGbufferPixelShaderOutput PixelShaderFunction_Holo(VertexShaderOutput_DNS input)
{
	float2 texCoord = GetScreenSpaceTextureCoord(input.BaseOutput.ScreenPosition, HalfPixel) * Scale;
	/*float nearDepth = DecodeFloatRGBA(tex2D(DepthTextureNearSampler, texCoord));
	float farDepth = DecodeFloatRGBA(tex2D(DepthTextureFarSampler, texCoord));

	float depth = min(nearDepth, farDepth) * FAR_PLANE_DISTANCE;
	if (depth + 0.01f < input.BaseOutput.TexCoordAndViewDistance.z)
		discard;*/

	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.BaseOutput.TexCoordAndViewDistance.xy);

	
	input.TangentToWorld[0] = normalize(input.TangentToWorld[0]);
	input.TangentToWorld[1] = normalize(input.TangentToWorld[1]);
	input.TangentToWorld[2] = normalize(input.TangentToWorld[2]);
    
    float4 normal = GetNormalVectorFromRenderTarget(tex2D(TextureNormalSampler, input.BaseOutput.TexCoordAndViewDistance.xy));
    normal.xyz = normalize(mul(normal.xyz, input.TangentToWorld));    
    							  
	return CalculateOutputHolo(input.BaseOutput, normal);
}

MyGbufferPixelShaderOutput PixelShaderFunction_Holo_IgnoreDepth(VertexShaderOutput_DNS input)
{
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.BaseOutput.TexCoordAndViewDistance.xy);

	input.TangentToWorld[0] = normalize(input.TangentToWorld[0]);
	input.TangentToWorld[1] = normalize(input.TangentToWorld[1]);
	input.TangentToWorld[2] = normalize(input.TangentToWorld[2]);
    
    float4 normal = GetNormalVectorFromRenderTarget(tex2D(TextureNormalSampler, input.BaseOutput.TexCoordAndViewDistance.xy));
    normal.xyz = normalize(mul(normal.xyz, input.TangentToWorld));    
    							  
	return CalculateOutputHolo(input.BaseOutput, normal);
}


MyGbufferPixelShaderOutput PixelShaderFunction_DNS_Masked(VertexShaderOutput_DNS input)
{
	static const float THR_NEAR = 0.5;
	static const float THR_FAR = 0.25;
	static const float THR_DISTANCE = 4000.f;
	float ndist = input.BaseOutput.TexCoordAndViewDistance.z / THR_DISTANCE;
	float thr = lerp(THR_NEAR, THR_FAR, saturate(ndist));
	float4 diffuseTexture = tex2D(TextureDiffuseSampler, input.BaseOutput.TexCoordAndViewDistance.xy);

	if (diffuseTexture.a < thr)
		discard;

    return PixelShaderFunction_DNS_Base(input, DiffuseColor, float3(SpecularIntensity, SpecularPower, Emissivity), Highlight, 0);
}

MyGbufferPixelShaderOutput PixelShaderFunction_Stencil(VertexShaderOutput_DNS input)
{
	return GetGbufferPixelShaderOutput(float3(0,0,0), float3(0,0,0), input.BaseOutput.TexCoordAndViewDistance.z);
}

MyGbufferPixelShaderOutput PixelShaderFunction_Stencil_Low(VertexShaderOutputLow_DNS input)
{
	return GetGbufferPixelShaderOutput(float3(0,0,0), float3(0,0,0), input.TexCoordAndViewDistance.z);
}


technique Technique_RenderQualityLow
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunctionLow_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunctionLow_DNS();
    }
}

technique Technique_RenderQualityNormal
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityHigh
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 
		
        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityExtreme
{
    pass Pass1
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
        MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
        MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
        MaxAnisotropy[2] = 16;

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_Holo
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_Holo();
    }
}

technique Technique_Holo_IgnoreDepth
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_Holo_IgnoreDepth();
    }
}

technique Technique_Stencil
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_Stencil();
    }
}
			 	
technique Technique_StencilLow
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 
        
        VertexShader = compile vs_3_0 VertexShaderFunctionLow_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_Stencil_Low();
    }
}	 	

technique Technique_RenderQualityNormalBlended
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Blended();
    }
}

technique Technique_RenderQualityHighBlended
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Blended();
    }
}

technique Technique_RenderQualityExtremeBlended
{
    pass Pass1
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
        MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
        MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
        MaxAnisotropy[2] = 16;

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Blended();
    }
}


technique Technique_RenderQualityLowMasked
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunctionLow_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Masked();
    }
}

technique Technique_RenderQualityNormalMasked
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Masked();
    }
}

technique Technique_RenderQualityHighMasked
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Masked();
    }
}

technique Technique_RenderQualityExtremeMasked
{
    pass Pass1
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
        MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
        MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
        MaxAnisotropy[2] = 16;

        VertexShader = compile vs_3_0 VertexShaderFunction_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Masked();
    }
}


technique Technique_RenderQualityHighSkinned
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 
		
        VertexShader = compile vs_3_0 VertexShaderFunctionSkinned_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityNormalInstanced
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunctionInstanced_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}
		
technique Technique_RenderQualityNormalInstancedSkinned
{
    pass Pass1
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 

        VertexShader = compile vs_3_0 VertexShaderFunctionInstancedSkinned_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityHighInstanced
{
    pass Pass1 
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstanced_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityHighInstancedSkinned
{
    pass Pass1 
    {
        MinFilter[0] = LINEAR; 
        MagFilter[0] = LINEAR; 

        MinFilter[1] = LINEAR; 
        MagFilter[1] = LINEAR; 

        MinFilter[2] = LINEAR; 
        MagFilter[2] = LINEAR; 
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstancedSkinned_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityExtremeSkinned
{
    pass Pass1
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
		MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
		MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
		MaxAnisotropy[2] = 16;
		
        VertexShader = compile vs_3_0 VertexShaderFunctionSkinned_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}
		
technique Technique_RenderQualityExtremeInstanced
{
    pass Pass1 
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
		MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
		MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
		MaxAnisotropy[2] = 16;
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstanced_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_RenderQualityExtremeInstancedSkinned
{
    pass Pass1 
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
		MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
		MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
		MaxAnisotropy[2] = 16;
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstancedSkinned_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_InstancedGeneric
{
    pass Pass1 
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
		MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
		MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
		MaxAnisotropy[2] = 16;
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstancedGeneric_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS();
    }
}

technique Technique_InstancedGenericMasked
{
    pass Pass1 
    {
        MinFilter[0] = ANISOTROPIC; 
        MagFilter[0] = LINEAR; 
		MaxAnisotropy[0] = 16;

        MinFilter[1] = ANISOTROPIC; 
        MagFilter[1] = LINEAR; 
		MaxAnisotropy[1] = 16;

        MinFilter[2] = ANISOTROPIC; 
        MagFilter[2] = LINEAR; 
		MaxAnisotropy[2] = 16;
		
        VertexShader = compile vs_3_0 VertexShaderFunctionInstancedGeneric_DNS();
        PixelShader = compile ps_3_0 PixelShaderFunction_DNS_Masked();
    }
}

