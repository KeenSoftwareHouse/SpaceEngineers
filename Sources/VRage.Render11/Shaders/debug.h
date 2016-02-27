#include <postprocess_base.h>
#include <gbuffer.h>
#include <Math/math.h>
#include <Math/Color.h>
#include <csm.h>
#include <EnvAmbient.h>

Texture2D<float4> DebugTexture : register( t0 );
Texture3D<float4> DebugTexture3D : register( t0 );
Texture2DArray<float> DebugTextureArray : register( t0 );

cbuffer DebugConstants : register(b5) {
	float SliceTexcoord;
};

struct ScreenVertex {
	float2 position : POSITION;
	float2 texcoord : TEXCOORD;
};
