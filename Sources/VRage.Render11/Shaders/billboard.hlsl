#include <common.h>
#include <frame.h>
#include <math.h>
#include <csm.h>
#include <EnvAmbient.h>

struct BillboardData
{
	int custom_projection_id;
	uint color_rgba;
	float reflective;
	float _padding0;
	float3 normal;
	float _padding1;
};

cbuffer CustomProjections : register ( b2 )
{
	matrix view_projection[32];
};

#define BILLBOARD_BUFFER_SLOT 104

StructuredBuffer<BillboardData> BillboardBuffer : register( MERGE(t,BILLBOARD_BUFFER_SLOT) );
Texture2D<float4> TextureAtlas : register( t0 );

Texture2D<float> Depth : register( t1 );

struct VsIn
{
	float3 position : POSITION;
	float2 texcoord : TEXCOORD0;
};

struct VsOut
{
	float4 position : SV_Position;
	float2 texcoord : Texcoord0;
	uint   index 	: Texcoord1;
	float3 wposition : TEXCOORD2;
#ifdef LIT_PARTICLE
	float3 light 	: Texcoord3;
#endif
};

#define REFLECTIVE

VsOut vs(VsIn vertex, uint vertex_id : SV_VertexID)
{
	VsOut result;

	uint billboard_index = vertex_id / 4;
	uint billboard_quad_index = vertex_id % 4;

	int custom_id = BillboardBuffer[billboard_index].custom_projection_id;
	float4 projPos; 
	if(custom_id < 0)	
	{
		projPos = mul(float4(vertex.position.xyz, 1), frame_.view_projection_matrix);	
	}
	else{
		projPos = mul(float4(vertex.position.xyz, 1), view_projection[custom_id]);
	}
	result.position = projPos;
	result.index = billboard_index;
	
	// result.texcoord = float2(
	// 	(billboard_quad_index == 1 || billboard_quad_index == 2) ? 1 : 0,
	// 	(billboard_quad_index / 2) ? 1 : 0);

	result.texcoord = vertex.texcoord;

	// float4 uv_modifiers;
	// uv_modifiers.x = f16tof32(BillboardBuffer[billboard_index].uv_modifiers_offset);
	// uv_modifiers.y = f16tof32(BillboardBuffer[billboard_index].uv_modifiers_offset >> 16);
	// uv_modifiers.z = f16tof32(BillboardBuffer[billboard_index].uv_modifiers_scale);
	// uv_modifiers.w = f16tof32(BillboardBuffer[billboard_index].uv_modifiers_scale >> 16);

	
	// result.texcoord = result.texcoord * uv_modifiers.zw + uv_modifiers.xy;
	result.wposition = vertex.position.xyz;

	float3 vs_pos = mul(float4(vertex.position.xyz, 1), frame_.view_matrix).xyz; 

#ifdef LIT_PARTICLE
	float3 V = normalize(get_camera_position() - vs_pos);
	result.light = frame_.directionalLightColor * calculate_shadow_fast_particle(vertex.position.xyz, -projPos.z / projPos.w) + ambient_diffuse(V);
#endif

	return result;
}

#pragma warning( disable : 3571 )


float4 ps(VsOut vertex) : SV_Target0
{
	//return float4(vertex.texcoord, 0, 1);

	float4 sample = TextureAtlas.Sample(LinearSampler, vertex.texcoord.xy);
	//return float4(vertex.texcoord.xy, 0, 1);
	sample.xyz = pow(sample.xyz, 2.2f);
	//float3 color_srgb = saturate(sample.rgb / sample.a);
 	//float3 color = pow(color_srgb, 2.2f);
 	//float4 linear_color = float4(color, 1) * sample.a;
	//float4 linear_color = sample;
	//linear_color.xyz = pow(abs(sample.xyz), 2.2f);

	float depth_sample = Depth[vertex.position.xy].r;
	float fade = 1;
	if(depth_sample < 1) {
		float targetdepth = linearize_depth(depth_sample, frame_.projection_matrix);
		float depth = linearize_depth(vertex.position.z, frame_.projection_matrix);
		fade = saturate((depth - targetdepth) * 5.0f);
	}

	float4 billboard_color = D3DX_R8G8B8A8_UNORM_SRGB_to_FLOAT4_inexact(BillboardBuffer[vertex.index].color_rgba);

#ifdef REFLECTIVE

	float reflective = BillboardBuffer[vertex.index].reflective;
	if(reflective)
	{
		//billboard_color.xyz = pow(billboard_color.xyz, 2.2f);
		float3 N = normalize(BillboardBuffer[vertex.index].normal);
		float3 V = normalize(get_camera_position() - vertex.wposition);
		float3 r = ambient_specular(0.04f, 0.95f, N, V);
		float3 c = lerp(billboard_color.xyz * billboard_color.w, r, reflective);

		float4 dirt = sample;
		float3 cDirt = lerp(c, dirt.xyz, dirt.w);

		float4 finalR = float4(cDirt, max(dirt.w, billboard_color.w));
		finalR.xyz *= 0.1;
		return finalR;	
	}

#endif
	//sample.xyz = pow(sample.xyz, 2.2);
	//billboard_color.xyz = pow(billboard_color.xyz, 2.2);
	float4 finalc = sample * billboard_color;

#ifdef LIT_PARTICLE
	finalc.xyz *= vertex.light;
#endif

	float4 c = float4(finalc * fade);
	return c;
}


// // SE
// float4 ps(VsOut vertex) : SV_Target0
// {
// 	float4 sample = TextureAtlas.Sample(LinearSampler, vertex.texcoord.xy);
// 	float4 linear_color = sample;
// 	linear_color.xyz = pow(abs(sample.xyz), 2.2f);

// 	float depth_sample = Depth[vertex.position.xy].r;
// 	float fade = 1;
// 	if(depth_sample < 1) {
// 		float targetdepth = linearize_depth(depth_sample, frame_.projection_matrix);
// 		float depth = linearize_depth(vertex.position.z, frame_.projection_matrix);
// 		fade = saturate((depth - targetdepth) * 5.f);
// 	}

// 	float4 billboard_color = D3DX_R8G8B8A8_UNORM_to_FLOAT4(BillboardBuffer[vertex.index].color_rgba);
// 	billboard_color.xyz = pow(billboard_color.xyz, 2.2f);

// 	//return float4(linear_color * billboard_color * fade);

// #ifdef REFLECTIVE

// 	float reflective = BillboardBuffer[vertex.index].reflective;
// 	if(reflective)
// 	{
// 		billboard_color.xyz = pow(billboard_color.xyz, 2.2f);
// 		float3 N = normalize(BillboardBuffer[vertex.index].normal);
// 		float3 V = normalize(get_camera_position() - vertex.wposition);
// 		float3 r = ambient_specular(0.04, 0.95f, N, V);
// 		float3 c = lerp(billboard_color.xyz, r, reflective);

// 		float4 dirt = linear_color;
// 		float3 cDirt = lerp(c, dirt.xyz, dirt.w);

// 		return float4(cDirt, max(dirt.w, billboard_color.w));	
// 	}

// #endif

// #ifdef LIT_PARTICLE
// 	billboard_color.xyz *= vertex.light;
// #endif
// 	return float4(linear_color * billboard_color * fade);
// }


// /*ME*/
// float4 ps(VsOut vertex) : SV_Target0
// {
// 	float4 sample = TextureAtlas.Sample(LinearSampler, vertex.texcoord.xy);

// 	float3 color_srgb = saturate(sample.rgb / sample.a);
// 	float3 color = pow(color_srgb, 2.2f);
// 	float4 linear_color = float4(color, 1) * sample.a;

// 	float depth_sample = Depth[vertex.position.xy].r;
// 	float fade = 1;
// 	if(depth_sample < 1) {
// 		float targetdepth = linearize_depth(depth_sample, frame_.projection_matrix);
// 		float depth = linearize_depth(vertex.position.z, frame_.projection_matrix);
// 		fade = saturate((depth - targetdepth) * 5.f);
// 	}

// 	float4 billboard_color = D3DX_R8G8B8A8_UNORM_to_FLOAT4(BillboardBuffer[vertex.index].color_rgba);
// 	//billboard_color.xyz = pow(billboard_color.xyz, 2.2f);

// #ifdef REFLECTIVE

// 	float reflective = BillboardBuffer[vertex.index].reflective;
// 	if(reflective)
// 	{
// 		billboard_color.xyz = pow(billboard_color.xyz, 2.2f);
// 		float3 N = normalize(BillboardBuffer[vertex.index].normal);
// 		float3 V = normalize(get_camera_position() - vertex.wposition);
// 		float3 r = ambient_specular(0.04, 0.95f, N, V);
// 		float3 c = lerp(billboard_color.xyz, r, reflective);

// 		float4 dirt = linear_color;
// 		float3 cDirt = lerp(c, dirt.xyz, dirt.w);

// 		return float4(cDirt, max(dirt.w, billboard_color.w));	
// 	}

// #endif

// #ifdef LIT_PARTICLE
// 	billboard_color.xyz *= vertex.light;
// #endif
// 	return float4(linear_color * billboard_color * fade);
// }