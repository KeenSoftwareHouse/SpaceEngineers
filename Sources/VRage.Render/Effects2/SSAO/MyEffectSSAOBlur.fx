
#include "../MyEffectBase.fxh"

float2 HalfPixel;
float2 SSAOHalfPixel;
float2 BlurDirection;
float samplesDiff;

texture DepthsRT;
sampler2D DepthsRTSampler = sampler_state
{
	Texture = <DepthsRT>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
};

Texture NormalsRT;
sampler NormalsRTSampler = sampler_state 
{ 
	texture = <NormalsRT> ; 
	magfilter = LINEAR; 
	minfilter = LINEAR; 
	mipfilter = NONE; 
	AddressU = CLAMP; 
	AddressV = CLAMP;
};

texture SsaoRT;
sampler2D SsaoRTSampler = sampler_state
{
	Texture = <SsaoRT>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
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
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 PixelShaderFunction(float2 TexCoord :TEXCOORD0) : COLOR0
{
    float depth = tex2D(DepthsRTSampler, TexCoord).x * FAR_PLANE_DISTANCE;    
    float3 normal = normalize(tex2D(NormalsRTSampler, TexCoord + HalfPixel).xyz * 2.0 - 1.0);	//TODO: Do we have to normalize it here?    
    float color = tex2D(SsaoRTSampler, TexCoord + SSAOHalfPixel).r;
   
    float num = 1;
    int blurSamples = 8; 
	
	for( int i = -blurSamples/2; i <= blurSamples/2; i+=1)
	{
		float2 newTexCoord = float2(TexCoord + i * BlurDirection.xy);
		
		float sample = tex2D(SsaoRTSampler, newTexCoord + SSAOHalfPixel).a;
		
		//float3 samplenormal = tex2D(DepthMap, newTexCoord + SSAOHalfPixel).rgb;
		float3 samplenormal = normalize(tex2D(NormalsRTSampler, newTexCoord + HalfPixel).xyz * 2.0 - 1.0);	//TODO: Do we have to normalize it here?    
			
		if (dot(samplenormal, normal) > samplesDiff)	
		{
			num += (blurSamples/2 - abs(i));
			color += sample * (blurSamples/2 - abs(i));
		}
	}

	return float4(0,0,0, color / num);
}

technique SSAOBlur
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}