#include "Debug.hlsli"

void __pixel_shader(float4 position : SV_Position, float2 texcoord : TEXCOORD0, out float4 color : SV_Target0) 
{
	color = DebugTexture3D.Sample(LinearSampler, float3(texcoord, SliceTexcoord) );
}

