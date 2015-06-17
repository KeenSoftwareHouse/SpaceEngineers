//#include "../MyEffectBase.fxh"
#include "../MyEffectDynamicLightingBase.fxh"
#include "../MyEffectReflectorBase.fxh"

float3 VoxelMapPosition;
float4x4 ViewProjectionMatrix;
float4x4 WorldMatrix;
float FadeoutDistance;
float4 EmissiveColor = float4(1.0f, 0.2f, 0.02f, 1.0f);


Texture DecalDiffuseTexture;
sampler DecalDiffuseTextureSampler = sampler_state 
{ 
	texture = <DecalDiffuseTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

Texture DecalNormalMapTexture;
sampler DecalNormalMapTextureSampler = sampler_state 
{ 
	texture = <DecalNormalMapTexture> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = LINEAR; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float4 Normal : NORMAL0;
    float4 Tangent : TANGENT0;
	float2 EmissiveRatio : TEXCOORD1;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
	float EmissiveRatio: TEXCOORD2;
	float3x3 TangentToWorld    : TEXCOORD3;
};


VertexShaderOutput VertexShaderFunction_VoxelDecals(VertexShaderInput input)
{
    VertexShaderOutput output;

	input.Position = input.Position;
	input.Normal = UnpackNormal(input.Normal);
	input.Tangent = UnpackNormal(input.Tangent);

	float4 worldPosition = float4(input.Position + VoxelMapPosition, 1);
	
    output.Position = mul(worldPosition, ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;

	float range = FadeoutDistance / 10;
	float fadeout = saturate( (range - max(0, abs(output.Position.z)-FadeoutDistance) ) / range );
    output.Color = input.Color;
	output.Color.a *= fadeout;

    output.TangentToWorld[0] = input.Tangent;
    output.TangentToWorld[1] = cross(input.Tangent,input.Normal);
    output.TangentToWorld[2] = input.Normal.xyz;

	output.EmissiveRatio = input.EmissiveRatio.x;

    return output;
}

VertexShaderOutput VertexShaderFunction_ModelDecals(VertexShaderInput input)
{
    VertexShaderOutput output;

	input.Position = input.Position;
	input.Normal = UnpackNormal(input.Normal);
	input.Tangent = UnpackNormal(input.Tangent);

    float4 worldPosition = mul(input.Position, WorldMatrix);    
    
    output.Position = mul(worldPosition, ViewProjectionMatrix);    
    output.TexCoord = input.TexCoord;

	float range = FadeoutDistance / 10;
	float fadeout = saturate( (range - max(0, /*abs*/(output.Position.z)-FadeoutDistance) ) / range );
    output.Color = input.Color;
	output.Color.a *= fadeout;
	output.EmissiveRatio = input.EmissiveRatio.x;
	
    output.TangentToWorld[0] = mul(input.Tangent.xyz, WorldMatrix);
    output.TangentToWorld[1] = mul(cross(input.Tangent.xyz,input.Normal.xyz), WorldMatrix);
    output.TangentToWorld[2] = mul(input.Normal.xyz, WorldMatrix);

    return output;
}

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{

	//return GetGbufferPixelShaderOutputBlended(float4(1,0,0,0), float4(1,0,0,1), 1,  0);

	float4 diffuse = tex2D(DecalDiffuseTextureSampler, input.TexCoord);

	//This is optimization for large decals where most of the pixels are totaly transparent
	if (diffuse.w <= 0.0) 
    {
		discard;
    }	 

	float4 normalAndNormalAlpha = tex2D(DecalNormalMapTextureSampler, input.TexCoord);
	float3 normal = GetNormalVectorFromDDS(normalAndNormalAlpha).xyz;
	float normalAlpha = normalAndNormalAlpha.w;

	input.TangentToWorld[0] = normalize(input.TangentToWorld[0]);
	input.TangentToWorld[1] = normalize(input.TangentToWorld[1]);
	input.TangentToWorld[2] = normalize(input.TangentToWorld[2]);
    normal.xyz = normalize(mul(normal.xyz, input.TangentToWorld)); 
	normal = input.TangentToWorld[2];

	diffuse = diffuse * input.Color;

	float emissivity = saturate(diffuse.w * diffuse.w * diffuse.w * diffuse.w - 0.2f);

	diffuse = lerp(diffuse, emissivity * EmissiveColor, input.EmissiveRatio);
	emissivity = lerp(0, diffuse.w, input.EmissiveRatio);

	//	Output into MRT
	MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutputBlended(float4(normal.xyz, normalAlpha), diffuse, emissivity,  0);
	//MyGbufferPixelShaderOutput output = GetGbufferPixelShaderOutputBlended(normal.xyz, float4(input.Color.www, 1), 0,  0);
	return output;
}



technique TechniqueVoxelDecals
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction_VoxelDecals();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

technique TechniqueModelDecals
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderFunction_ModelDecals();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

