//	Clears all render targets in our MRT to desired values

#include "../MyEffectBase.fxh"

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    //	We're using a full screen quad, no need for transforming vertices.
    VertexShaderOutput output;
    output.Position = input.Position;
    return output;
}

MyGbufferPixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{    
    //	Output into MRT
	MyGbufferPixelShaderOutput output;

	//	Normals are mapped from [-1..1] to [0..1]. This means we need to map the neutral value to [0..1] as well.
    output.NormalAndSpecPower.xyz = GetNormalVectorIntoRenderTarget(float3(0,0,0));
	output.NormalAndSpecPower.w = 1;

	//	Black color - zero alpha is needed for good LOD (but only if we output LOD alpha in diffuse RT, which is unlikely)
	output.DiffuseAndSpecIntensity = float4(0, 0, 0, 0);
	
	//	Clear to maximum depth.
    output.DepthAndEmissivity = EncodeFloatRGBA(0.99999f);
	output.DepthAndEmissivity.w = PackGBufferEmissivityReflection(0, 0.0f); // zero emissivity, zero reflection
	
    return output;    
}

technique Technique1
{
    pass Pass1
    {
		VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}