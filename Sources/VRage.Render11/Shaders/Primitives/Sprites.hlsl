Texture2D	SpriteTexture	: register( t0 );

#include <Math/Color.hlsli>
#include <Common.hlsli>
#include <Frame.hlsli>
#include <GBuffer/GBufferWrite.hlsli>

struct TargetConstants {
	float2 resolution;
};

cbuffer Target : register (MERGE(b,PROJECTION_SLOT))
{
	TargetConstants Target;
};

struct VertexInput
{
	float4 clipspace_offset_scale : TEXCOORD0; // scale is in screen coords
	float4 texcoord_offset_scale : TEXCOORD1;
	float4 origin_rotation : TEXCOORD2; // origin of rotation is in [-0.5x0.5] square
	float4 color : COLOR;
};

struct ProcessedVertex
{
	float4 position : SV_Position;
	float4 color : COLOR;
	float2 texcoord0 : TEXCOORD0;
};

void __vertex_shader(
	uint vertex_id : SV_VertexID, 
	uint instance_id : SV_InstanceID, 
	VertexInput input,
	out ProcessedVertex output) 
{
	float2 quad_pos = float2(-0.5f + (vertex_id > 1), -0.5f + (vertex_id & 1));
	float2 quad_uv = float2((vertex_id > 1), (~vertex_id & 1));

	float2 B = cross(float3(0,0,1), float3(input.origin_rotation.zw, 0)).xy;
	float2x2 rotation = float2x2(input.origin_rotation.zw, B);
	rotation._12_21 *= -1; // ss to cs

	output.position.xy = mul(quad_pos - input.origin_rotation.xy, rotation) * 2 * input.clipspace_offset_scale.zw / Target.resolution + input.clipspace_offset_scale.xy;
	output.position.zw = float2(0, 1);

	output.texcoord0 = quad_uv * input.texcoord_offset_scale.zw + input.texcoord_offset_scale.xy;
    output.color = srgba_to_rgba(input.color);
    output.color.rgb *= output.color.a;
}


void __pixel_shader(ProcessedVertex input, out float4 output : SV_Target0) 
{
	float4 sample = SpriteTexture.Sample(TextureSampler, input.texcoord0);
    sample.rgb *= sample.a;
	output = sample * input.color;
}
