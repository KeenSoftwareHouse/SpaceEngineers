#include <postprocess_base.h>
#include <gbuffer.h>
#include <math.h>
#include <csm.h>
#include <EnvAmbient.h>

void base_color(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(input.base_color.xyz, 1);
}

void base_color_linear(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.base_color.xyz), 1);
}

void normal(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.N * 0.5 + 0.5), 1);
}

void glossiness(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.gloss), 1);
}

void metalness(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.metalness), 1);
}

void mat_id(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.id), 1);
}

void ambient_occlusion(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.ao), 1);
}

void emissive(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.emissive), 1);
}

void debug_ambient_diffuse(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	float3 albedo = surface_albedo(input.base_color, input.metalness);
	output = float4(input.ao * ambient_diffuse(input.N) * albedo, 1);
}

void debug_ambient_specular(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	output = float4(srgb_to_rgb(input.gloss), 1);

	if(input.native_depth == 1)
		discard;

	float3 N = input.N;
	float3 V = input.V;

	float3 f0 = surface_f0(input.base_color, input.metalness);
	output = float4(input.ao * ambient_specular(f0, input.gloss, N, V), 1);
}

void debug_edge(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	output = 0;
	if(gbuffer_edgedetect(vertex.position.xy)) {
		output = float4(1,0,0,1);
	}
	else {
		discard;
	}
}

static const float3 cascadeColor[] = {
	float3(1,0,0),
	float3(0,1,0),
	float3(0,0,1),
	float3(1,1,0),

	float3(0,1,1),
	float3(1,0,1),
	float3(1,0,0.5),
	float3(0.5,1,0),
};

Texture2D<float> Shadows : register( MERGE(t,SHADOW_SLOT) );

void cascades_shadow(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	SurfaceInterface input = read_gbuffer(vertex.position.xy);

	float shadow = calculate_shadow(input.position, input.stencil);
	output = float4(cascadeColor[cascade_id_stencil(input.stencil)] * shadow, 1);
}


Texture2D<float4> DebugTexture : register( t0 );
Texture3D<float4> DebugTexture3D : register( t0 );

cbuffer DebugConstants : register(b5) {
	float SliceTexcoord;
};

struct ScreenVertex {
	float2 position : POSITION;
	float2 texcoord : TEXCOORD;
};

void screenVertex(ScreenVertex vertex, out float4 position : SV_Position, out float2 texcoord : TEXCOORD0) {
	float2 xy = vertex.position / frame_.resolution;
	xy = xy * 2 - 1;
	xy.y = -xy.y;

	position = float4(xy, 0, 1);
	texcoord = vertex.texcoord;
}

void blitTexture(float4 position : SV_Position, float2 texcoord : TEXCOORD0, out float4 color : SV_Target0) {
	color = DebugTexture.Sample(LinearSampler, texcoord);
}

void blitTexture3D(float4 position : SV_Position, float2 texcoord : TEXCOORD0, out float4 color : SV_Target0) {
	color = DebugTexture3D.Sample(LinearSampler, float3(texcoord, SliceTexcoord) );
}