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


Texture NormalsTexture;
sampler NormalsTextureSampler = sampler_state 
{ 
	texture = <NormalsTexture> ; 
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
	float2 ScreenPosition2 : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.TexCoord = input.TexCoord + HalfPixel;
	output.ScreenPosition = input.Position;
	
	output.ScreenPosition2 = (input.Position.xy + 1.0) / 2.0;
    output.ScreenPosition2.y = 1.0 - output.ScreenPosition2.y;
	return output;
}


float4 AO_RAND[32]; // random offset vectors (3 dimensional in tangent space)
#define interleaved false
#define removesiluette false
#define weightedIS false
#define samplecount 8

/// Transformation matrices
float4x4 projMatrix;
float4x4 projMatrixInverse;
float4x4 ViewMatrix;

float offset = 1.0f;
float siluetteDist = 3.0f;
float R = 1;

  /*
///Common Vertex shader
void vsFSQuad(in float4 pos: POSITION,
        out float4 hPosition: POSITION,
        out float2 wPos: TEXCOORD0,
        out float2 hPos: TEXCOORD1)
{
    hPosition = float4(pos.xyz,1);
    hPos = hPosition.xy;
    wPos = (hPosition.xy + 1.0) / 2.0;
    wPos.y = 1.0 - wPos.y;
}
*/

float4 psSSAOVolumetric(in float2 wPos :TEXCOORD0, float2 hPos: TEXCOORD1,float2 vPos : VPOS) : COLOR0
{  
    float depth = tex2D(DepthsRTSampler,vPos).x * FAR_PLANE_DISTANCE; // Camera spacedepth
   // if (depth >= 1000.0) return 0.5;
    float3 N;
   // N.xy = tex2D(DepthsRTSampler,vPos).yz; // Camera spacenormal x,y coordinate
   // N.z = -sqrt(1.0 - N.x * N.x - N.y * N.y); // Compute normalz coordinate

	N = GetNormalVectorFromRenderTargetNormalized(tex2D(NormalsTextureSampler,vPos).xyz);
	N = normalize(mul(N, ViewMatrix));
   
    // Compute camera space position from normalized devicecoordinates and camera space depth
    float3 cPos = float3(hPos, 0);
    float4 tempp = mul(float4(cPos,1), projMatrixInverse);
    tempp.xyz /= tempp.w;
    cPos = tempp.xyz * depth / tempp.z;
    // This coordinate is used to read from a 4x4 randomtextured tiled onto the screen
    float2 screenCoord = vPos.xy / 4;
    // Read random texture   
    float2x2 rr;
    if(interleaved)
    {
        float3 AO_RANDOM =tex2D(RandomTextureSampler, screenCoord).xyz;
        // Build a tangent space rotationmatrix from the random vector
        float2 r1 = normalize(AO_RANDOM.xy);
        float2 r2 = float2(r1.y, -r1.x);
        rr = float2x2(r1,r2);       
    }
    float Zcap = AO_RAND[samplecount - 1].z;
    float Rhalf = 0.5 * R;
    float W = 0;
     
    for (int k = 0; k < samplecount; k++)
    {
        float2 rdir = AO_RAND[k].xy;
       
        if(interleaved)
            rdir =mul(rdir, rr);       
       
        float xi = rdir.x;
        float yi = rdir.y;
       
        float3 p = cPos + Rhalf * (N +float3(xi,yi,0));
        float pz = p.z;
        float D = Rhalf * sqrt(1 - xi*xi -yi*yi);
        float2 tmpCoord;
        float4 tmpHPos = mul(float4(p, 1),projMatrix);
        tmpCoord = tmpHPos.xy / tmpHPos.w;
        tmpCoord.y *= -1;
        tmpCoord = (tmpCoord + 1.0) *0.5;       
        float z = tex2D(DepthsRTSampler,tmpCoord).r * FAR_PLANE_DISTANCE;
                 
        float dZi = min(pz + D, z) - min(z,pz - D);
        if(z < pz - D) dZi = 0;
        if(removesiluette && z <pz - D - R * siluetteDist)
         dZi = 2*D;
         
        W +=dZi;       
    }
    if(weightedIS)
        W /= R * Zcap;
    else
        W /= R * samplecount * 2.0f / 3.0f;
       
    return float4(0,0,0,W);
	//return float4(0,0,0,depth * 1000);
}

/*
/// 4x4 averaging filter to blur the interleaved ambient occlusion map.
float4 psFSQuadBlur4x4(in float2 wPos :TEXCOORD0): COLOR
{
  float2 uv = wPos;
  float4 sum = float4(0,0,0,0);
  sum += tex2D(InputImage,uv);
  sum += tex2D(InputImage,uv + 2 * HalfPixel);
  sum += tex2D(InputImage,uv + float2(HalfPixel.x * 2, 0));
  sum += tex2D(InputImage,uv + float2(0, HalfPixel.y * 2));
  return sum /4.0;
 }
 
/// 4x4 averaging filter to blur the interleaved ambient occlusion map, butdoes not blur contour pixels.
float4 psFSQuadBlur4x4Contour(in float2 wPos :TEXCOORD0): COLOR
{ 
  float2 ppss = HalfPixel * 2.0;
  float2 uv = wPos;// + HalfPixel;
  float4 sum = float4(0,0,0,0);
  float centerdepth = tex2D(DepthsRTSampler, uv + 0.5 * HalfPixel).r;
  float4 centersample = tex2D(InputImage,uv + 0.5 * HalfPixel);
  float counter = 0;
  //float dMax = R * 0.2;
  float dMax = R * 0.8;
  if(abs(tex2D(DepthsRTSampler, uv).r - centerdepth) < dMax)
  {
  sum += tex2D(InputImage,uv);
  counter++;
  }
  if(abs(tex2D(DepthsRTSampler, uv + ppss).r - centerdepth) < dMax)
  {
   sum += tex2D(InputImage,uv + ppss);
   counter++;
  }
  if(abs(tex2D(DepthsRTSampler, uv + float2(ppss.x, 0)).r - centerdepth)< dMax)
  {
   sum += tex2D(InputImage,uv + float2(ppss.x, 0));
   counter++;
  }
  if(abs(tex2D(DepthsRTSampler, uv + float2(0, ppss.y)).r - centerdepth)< dMax)
  {
   sum += tex2D(InputImage,uv + float2(0, ppss.y));
   counter++;
  } 
  sum =  sum / counter;
 
  if(counter == 0)
  sum = centersample;
  return sum;
  }

static void generateAODirections()
    {
        if(AOdirections == 0)
            AOdirections = newD3DXVECTOR4[32];
        int i = 0;
        float Zcap = 0;
        while(i < 32)
        {           
            double x =halton1->getNext() * 2.0 - 1.0;
            double y =halton2->getNext() * 2.0 - 1.0;           
            double z =halton3->getNext() * 2.0 - 1.0;    
            
            if(sqrt(x*x + y*y)> 1)
               continue;
            Zcap += sqrt(1 - x*x -y*y);
            double z =Zcap;            
            
            AOdirections[i] =D3DXVECTOR4(x,y,z,1);
            i++;
        }
    }
  */
























float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	return psSSAOVolumetric(input.ScreenPosition2.xy, input.ScreenPosition, input.TexCoord);
	//return SSAO(input.TexCoord, input.ScreenPosition, false);
}


technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}
