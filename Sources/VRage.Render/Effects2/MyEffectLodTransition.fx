//	This shader blends between texture LOD0 and LOD1 according to distance or alpha (will depend on final implementation)

float2 HalfPixel;

//	Texture contains scene from LOD0
Texture Lod0RT;
sampler Lod0RTSampler = sampler_state 
{ 
	texture = <Lod0RT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
};

//	Texture contains scene from LOD1
Texture Lod1RT;
sampler Lod1RTSampler = sampler_state 
{ 
	texture = <Lod1RT> ; 
	magfilter = POINT; 
	minfilter = POINT; 
	mipfilter = NONE; 
	AddressU = clamp; 
	AddressV = clamp;
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
    //	We're using a full screen quad, no need for transforming vertices.
    VertexShaderOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord + HalfPixel;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 Lod0RT = tex2D(Lod0RTSampler, input.TexCoord);
	float4 Lod1RT = tex2D(Lod1RTSampler, input.TexCoord);
    
	//return float4(Lod0RT.rgb, 1);
    //return float4(Lod1RT.rgb, 1);
    
    //return float4(0,1,0,1);
    
    return float4(Lod0RT.rgb * Lod0RT.a + Lod1RT.rgb * (1 - Lod0RT.a), 1);    
    
    //return float4(Lod0RT.rgb + Lod1RT.rgb, 1);
    //return float4(Lod0RT.rgb * Lod0RT.a + Lod1RT.rgb * Lod1RT.a, 1);
    /*
    return float4(Lod1RT.rgb * (1 - Lod1RT.a), 1);
    
    float alpha0 = Lod0RT.a;
    float alpha1 = 1 - Lod1RT.a;
    float sum = alpha0 + alpha1;
    //alpha0 = alpha0 / sum;
    //alpha1 = alpha1 / sum;
    if (sum > 1)
    {
		alpha1 = 1 - alpha0;
    }
    
    return float4(Lod0RT.rgb * alpha0 + Lod1RT.rgb * alpha1, 1);
    */
    //return float4(Lod0RT.rgb * Lod0RT.a + Lod1RT.rgb * (1 - Lod1RT.a), 1);
    //return float4(Lod1RT.rgb * (1 - Lod1RT.a), 1);
}

technique Technique1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}