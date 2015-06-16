#include "../MyEffectBase.fxh"

float2 HalfPixel;
float4x4 ViewMatrix;

uniform float totStrength = 1.2;	//2.6		3.14	  1.423
uniform float strength = 0.7;		//4.8		5.4		  x
uniform float offset = 50.0;		//50		18		  50
uniform float falloff = 4.00;		//6			0		  6
uniform float rad = 0.00001;			//0.025		0.001	  0
uniform float shadingOffset = 0.5;						//0.248

#define SAMPLES 10 // 10 is good
const float invSamples = 1.0 / SAMPLES;


Texture NormalsRT;
sampler NormalsRTSampler = sampler_state 
{ 
	texture = <NormalsRT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
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
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture RandomTexture;
sampler RandomTextureSampler = sampler_state 
{ 
	texture = <RandomTexture> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = WRAP; 
	AddressV = WRAP;
};

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{				 /*
	// these are the random vectors inside a unit sphere
		const float3 pSphere[8] = {
		float3(1, 1, 1),
		float3(-1, -1, -1),
		float3(-1, -1, 1),
		float3(-1, 1, -1),
		float3(-1, 1, 1),
		float3(1, -1, -1),
		float3(1, -1, 1),
		float3(1, 1, -1)
		 };
  				   */
		 	// these are the random vectors inside a unit sphere
			
		const float3 pSphere[10] = {
		float3(-0.010735935, 0.01647018, 0.0062425877),
		float3(-0.06533369, 0.3647007, -0.13746321),
		float3(-0.6539235, -0.016726388, -0.53000957),
		float3(0.40958285, 0.0052428036, -0.5591124),
		float3(-0.1465366, 0.09899267, 0.15571679),
		float3(-0.44122112, -0.5458797, 0.04912532),
		float3(0.03755566, -0.10961345, -0.33040273),
		float3(0.019100213, 0.29652783, 0.066237666),
		float3(0.8765323, 0.011236004, 0.28265962),
		float3(0.29264435, -0.40794238, 0.15964167) };
			  

	float2 uv = input.TexCoord;

	// grab a normal for reflecting the sample rays later on
	float3 fres = normalize(tex2D(RandomTextureSampler, uv * offset).xyz * 2.0 - 1.0);//TODO: Do we have to normalize it here?    


	float currentPixelDepthNormalized = tex2D(DepthsRTSampler, input.TexCoord).x;
	float currentPixelDepth = currentPixelDepthNormalized * FAR_PLANE_DISTANCE;

	// get the normal of current fragment
	//float3 norm = currentPixelSample.xyz;
	float3 norm = normalize(tex2D(NormalsRTSampler, input.TexCoord).xyz * 2.0 - 1.0);//TODO: Do we have to normalize it here?    
	
	float3 norm2 = normalize(mul(norm, ViewMatrix));

	float bl = 0.0;

	// adjust for the depth ( not shure if this is good..)
	//float radD = saturate((rad / currentPixelDepth) * 100);
	float radD = rad;// * currentPixelDepth / 10.0f;	
	//float radD = lerp(0.02, 0.002, saturate(currentPixelDepth / 100));

	//view-space position
    float3 position;
    position.xy = input.ScreenPosition.xy * 0.5f + 0.5f;
	position.y = 1 - position.y;
    position.z = currentPixelDepthNormalized;

	for (int i = 0; i < SAMPLES; ++i)
	{
		// get a vector (randomized inside of a sphere with radius 1.0) from a texture and reflect it
		float3 ray = radD * reflect(pSphere[i], fres);

		ray = sign(dot(ray, norm2)) * ray.xyz;

		float3 hemPosition = position + ray;

		// if the ray is outside the hemisphere then change direction
		// float2 se = uv.xy + sign(dot(ray, norm2)) * ray.xyz;

		// get the depth of the occluder fragment
		//float4 occluderFragment = texture2D(normalMap,se.xy);		

		// get the normal of the occluder fragment
		//occNorm = occluderFragment.xyz;
		//float3 occNorm = normalize(tex2D(NormalsRTSampler, se.xy).xyz * 2.0 - 1.0);

		// if depthDifference is negative = occluder is behind current fragment
		
		//float occDepth = tex2D(DepthsRTSampler, se.xy).x * FAR_PLANE_DISTANCE;
		
		float occDepth = tex2D(DepthsRTSampler, hemPosition.xy).x * FAR_PLANE_DISTANCE;
		float hemDepth = hemPosition.z * FAR_PLANE_DISTANCE;
		
		//float sampledDepth = length(se.xyz);

		//float depthDifference = currentPixelDepth - occDepth;
		//float depthDifference = sampledDepth - occDepth;


		float diff = abs(hemDepth - occDepth);
		float range_check = diff < falloff ? 1.0 : 0.0;
		bl += (hemDepth <= occDepth ? 1.0 : 0.0) * range_check;


		//float diff = abs(currentPixelDepth - occDepth);
		//float range_check = diff < falloff ? 1.0 : 0.0;
		//bl += (currentPixelDepth <= occDepth ? 1.0 : 0.0) * range_check;



/*		
		// calculate the difference between the normals as a weight
		float normDiff = (1.0 - dot(occNorm, norm));
		
		// the falloff equation, starts at falloff and is kind of 1/x^2 falling
		bl += step(falloff, depthDifference) * normDiff * (1.0 - smoothstep(falloff, strength, depthDifference));
		*/
	}

	//float3 o = normalize(tex2D(NormalsRTSampler, position.xy).xyz * 2.0 - 1.0);

	// output the result
	float ao = 1.0 - totStrength * bl * invSamples + shadingOffset;
	
	//return float4(o, 1);
	return float4(0, 0, 0, 1 - ao);
}

technique Technique1
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
