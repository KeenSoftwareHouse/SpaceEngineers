#include "../MyEffectBase.fxh"


/*Values that are to be passed for getting ideal Light Scattering(Not Overdone)
Density = 0.34f;
Weight = 0.5f;
Decay = 0.97f;
 Exposition = 0.015f;
*/
 
Texture frameTex;
sampler2D frameSampler = sampler_state
{
	 Texture = <frameTex>;
	 ADDRESSU = CLAMP;
	 ADDRESSV = CLAMP;
	 MAGFILTER = POINT;
	 MINFILTER = POINT;
	 MIPFILTER = none;
};

Texture depthTex;
sampler2D DepthsRTSampler = sampler_state
{
	 Texture = <depthTex>;
	 ADDRESSU = CLAMP;
	 ADDRESSV = CLAMP;
	 MAGFILTER = POINT;
	 MINFILTER = POINT;
	 MIPFILTER = none;
};


float2 HalfPixel;
float3 FrustumCorners[4];

float4x4 View;
float4x4 WorldViewProjection;
float Density = 0.8f;
float Weight = 0.9f;
float Decay = 0.5f;
float Exposition = 0.5f;
float3 LightPosition;
float3 CameraPos;
static int numSamples = 18;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 TexCoordAndCornerIndex	: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;	
	float3 FrustumCorner : TEXCOORD1; 
};


VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoordAndCornerIndex.xy;// + HalfPixel;
	output.FrustumCorner = FrustumCorners[input.TexCoordAndCornerIndex.z];
	return output;
}


half4 PixelShaderFunction(VertexShaderOutput input, float2 screenPosition : VPOS) : COLOR0
{
    half4 screenPos = mul(LightPosition, WorldViewProjection); 
     
    half2 ssPos = screenPos.xy / screenPos.w * float2(0.5,-0.5) + 0.5;
   
	half2 oriTexCoord = input.TexCoord;
   
	half2 deltaTexCoord = (input.TexCoord - ssPos);
    
    deltaTexCoord *= 1.0f / numSamples * Density;
    
    half3 color = tex2D(frameSampler, input.TexCoord);
	//half3 color2 = color;
    
    half illuminationDecay = 1.0f;
    
	float2 texCoord = input.TexCoord;

	float depth	=  DecodeFloatRGBA(tex2D(DepthsRTSampler, texCoord));
	float viewDistance = GetViewDistanceFromDepth(depth, input.FrustumCorner.xyz);
	
	illuminationDecay = saturate(viewDistance / 1000);
	
	for (int i=0; i < numSamples; i++)
	{		
		float depth	=  DecodeFloatRGBA(tex2D(DepthsRTSampler, texCoord));
		float viewDistance = GetViewDistanceFromDepth(depth, input.FrustumCorner.xyz);

		half3 sample = tex2D(frameSampler, texCoord);
		

		if(viewDistance < 50)
		{
			sample = 0;
		}
		
		texCoord -= deltaTexCoord;
   
		sample *= illuminationDecay * Weight;
		color += sample;
		
		illuminationDecay *= Decay;
	} 

	//float4 smp = tex2D(frameSampler, input.TexCoord);
 
	half amount = dot(mul(LightDirection,View), half3(0.0f,0.0f,1.0f));
 
    //color = smoothstep(half3(0,0,0), half3(1,1,1), color); // Contrast
	return float4( saturate(amount-0.5) * color * Exposition, 1);
	//return float4(amount, amount, amount, 1);
	//return float4(amount.xxx,1);
}


technique Scatter
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

