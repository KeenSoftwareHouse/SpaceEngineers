#include "../MyEffectBase.fxh"

float2 HalfPixel;
float2 ScreenSize;
float3 CornerFrustum;

const float g_random_size = 64; 
const float g_sample_rad = 32;			//	the sampling radius.
const float g_intensity = 128;	//	the ao intensity.
const float g_scale = 1;		//	scales distance between occluders and occludee.
const float g_bias = 0.0;			//	controls the width of the occlusion cone considered by the occludee.

Texture NormalsRT;
sampler NormalsRTSampler = sampler_state 
{ 
	texture = <NormalsRT> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

Texture DepthsRT;
sampler DepthsRTSampler = sampler_state 
{ 
	texture = <DepthsRT> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
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


struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 ViewDirection : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.Position = input.Position;
    output.TexCoord = input.TexCoord + HalfPixel;
    
    float2 positionSign = sign(input.Position.xy);
    output.ViewDirection = float3(-CornerFrustum.x * positionSign.x, CornerFrustum.y * positionSign.y, CornerFrustum.z);

    return output;
}

float3 GetPosition(float2 texCoord, float3 viewDirection, float2 deltaTexCoord) 
{ 
	//return tex2D(g_buffer_pos, uv).xyz; 
	//return float3(1,1,1);
	
	float3 deltaDirection = float3(
		lerp(-CornerFrustum.x, +CornerFrustum.x, deltaTexCoord.x), 
		lerp(+CornerFrustum.y, -CornerFrustum.y, deltaTexCoord.y), 0);
	
	float viewDistanceNormalized = tex2D(DepthsRTSampler, texCoord + deltaTexCoord).x;
	float viewDistance = viewDistanceNormalized * FAR_PLANE_DISTANCE;
	return normalize(viewDirection + deltaDirection) * viewDistance;	
} 

float3 GetNormal(float2 texCoord) 
{ 
	return normalize(tex2D(NormalsRTSampler, texCoord).xyz * 2.0f - 1.0f); 
} 

float2 GetRandom(float2 texCoord) 
{ 
	return normalize(tex2D(RandomTextureSampler, ScreenSize * texCoord / g_random_size).xy * 2.0f - 1.0f); 
} 

float DoAmbientOcclusion(float2 tcoord, float2 uv, float3 p, float3 cnorm, float3 viewDirection) 
{ 
	float3 diff = GetPosition(tcoord, viewDirection, uv) - p; 
	const float3 v = normalize(diff); 
	const float d = length(diff) * g_scale; 
	return max(0.0, dot(cnorm, v) - g_bias) * (1.0 / (1.0 + d)) * g_intensity;
	//return max(0.0, dot(cnorm, v) - g_bias) * (1.0 / (1.0 + d)); 
	//return max(0.0, dot(cnorm, v) - g_bias) * g_intensity; 
	//return max(0.0, dot(cnorm, v) - g_bias);
} 

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 viewDirectionNormalized = input.ViewDirection;//normalize(input.ViewDirection);
	
	const float2 vec[4] = { float2(1,0), float2(-1,0), float2(0,1), float2(0,-1)}; 
	float3 p = GetPosition(input.TexCoord, viewDirectionNormalized, float2(0, 0)); 
	float3 n = GetNormal(input.TexCoord); 
	float2 rand = GetRandom(input.TexCoord); 
	float ao = 0.0f; 
	
	/*float viewDistanceNormalized = tex2D(DepthsRTSampler, input.TexCoord).x;
	float viewDistance = viewDistanceNormalized * FAR_PLANE_DISTANCE;
	
	float rad = g_sample_rad / viewDistance; 
	
	//	SSAO Calculation
	int iterations = 4; 
	for (int j = 0; j < iterations; ++j) 
	{ 
		float2 coord1 = reflect(vec[j], rand) * rad; 
		float2 coord2 = float2(coord1.x * 0.707 - coord1.y * 0.707, coord1.x * 0.707 + coord1.y * 0.707); 
		ao += DoAmbientOcclusion(input.TexCoord, coord1 * 0.25, p, n, viewDirectionNormalized); 
		ao += DoAmbientOcclusion(input.TexCoord, coord2 * 0.50, p, n, viewDirectionNormalized); 
		ao += DoAmbientOcclusion(input.TexCoord, coord1 * 0.75, p, n, viewDirectionNormalized); 
		ao += DoAmbientOcclusion(input.TexCoord, coord2, p, n, viewDirectionNormalized); 
	} 
	ao /= (float)iterations * 4.0; */
	
	//return float4(0, 0, 0, ao)

	return float4(ao, ao, ao, 1);
}

technique Technique1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}