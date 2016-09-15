#include <Postprocess/PostprocessBase.hlsli>
#include <GBuffer/GBuffer.hlsli>
#include <Math/Math.hlsli>
#include <Math/Color.hlsli>
#include <ShadowsOld/Csm.hlsli>
#include <Lighting/EnvAmbient.hlsli>

Texture2D<float4> DebugTexture : register( t0 );
Texture3D<float4> DebugTexture3D : register( t0 );
Texture2DArray<float> DebugTextureArray : register( t0 );

SamplerState BilinearSampler : register( s0 );

cbuffer DebugConstants : register(b5) 
{
	float SliceTexcoord;
};

struct ScreenVertex 
{
	float2 position : POSITION;
	float2 texcoord : TEXCOORD;
};
