#include "../MyEffectShadowBase.fxh"

float4x4	WorldMatrix;
float4x4	ViewProjMatrix;
float2		ShadowTermHalfPixel;
float3		FrustumCornersVS[4];
float2		HalfPixel;


// Vertex shader for outputting light-space depth to the shadow map
void GenerateShadowMapVS(	in float4 in_vPositionOS	: POSITION,
							out float4 out_vPositionCS	: POSITION,
							out float2 out_vDepthCS		: TEXCOORD0	)
{
	// Unpack position
	in_vPositionOS = in_vPositionOS;

	// Figure out the position of the vertex in view space and clip space
	out_vPositionCS = mul(in_vPositionOS, WorldMatrix);
    out_vPositionCS = mul(out_vPositionCS, ViewProjMatrix);
	out_vDepthCS = out_vPositionCS.zw;
}


// Pixel shader for outputting light-space depth to the shadow map
float4 GenerateShadowMapPS(in float2 in_vDepthCS : TEXCOORD0) : COLOR0
{
	// Negate and divide by distance to far clip (so that depth is in range [0,1])
	float fDepth = in_vDepthCS.x / in_vDepthCS.y;			
		
    return float4(fDepth, 1, 1, 1); 
}


technique GenerateShadowMap
{
	pass p0
	{
		VertexShader = compile vs_2_0 GenerateShadowMapVS();
        PixelShader = compile ps_2_0 GenerateShadowMapPS();
	}
}


