#include <EnvPrefiltering.h>

Texture2DArray<float3> Face0 : register( t0 );
Texture2DArray<float3> Face1 : register( t1 );
RWTexture2DArray<float3> BlendOutput : register( u0 );

[numthreads(8, 8, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint2 texel = dispatchThreadID.xy;

	if(any(texel >= MipResolution)) return;

	BlendOutput[uint3(texel, 0)] = lerp(Face0[uint3(texel, 0)].xyz, Face1[uint3(texel, 0)].xyz, Gloss_Blend);
	//BlendOutput[uint3(texel, 0)] = abs(Face0[uint3(texel, 0)].xyz - Face1[uint3(texel, 0)].xyz);
}
