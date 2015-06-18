static const int MAX_POINT_LIGHTS = 256;
static const int MAX_SPOTLIGHTS = 128;

#ifndef SAMPLE_FREQ_PASS
#define PIXEL_FREQ_PASS
#endif

#define FALOFF_TO_RADIUS

struct ProxyVertex
{
	float4 position : POSITION;
};

struct SpotlightConstants
{
	matrix  worldViewProj;
	matrix  shadowMatrix;

	float3 	position;
	float 	range;
	float3 	color;
	float 	apertureCos;
	float3	direction;
	float 	castsShadows;
	float3 	up;
};

cbuffer Spotlights : register( b1 )
{
	SpotlightConstants Spotlight;
}

struct PointLightData {
	float3 positionView;
	float range;
    float3 color;
    float radius;
};

#ifndef MAX_TILE_LIGHTS
#define MAX_TILE_LIGHTS 256
#endif

StructuredBuffer<PointLightData> LightList : register ( t13 );
StructuredBuffer<uint> TileIndices : register( t14 );
Texture2D<float3> ReflectorMask : register( t13 );
Texture2D<float> SpotlightShadowmap : register( t14 );

#include <brdf.h>
#include <postprocess_base.h>
#include <gbuffer.h>
#include <csm.h>
#include <LightingModel.h>
#include <EnvAmbient.h>
#include <vertex_transformations.h>

Texture2D<float> ShadowsMainView : register( MERGE(t,SHADOW_SLOT) );

void spotlightVs(float4 vertexPos : POSITION, out float4 svPos : SV_Position)
{
	svPos = mul(unpack_position_and_scale(vertexPos), Spotlight.worldViewProj);
}

static const float3 PCF_SHADOW_SAMPLES[] = {
	float3( -0.024989, 0.005582, 0.182518),
	float3( 0.677835, -0.380678, 0.143624),
	float3( 0.599357, 0.590492, 0.139817),
	float3( -0.694194, -0.617016, 0.134441),
	float3( -0.130402, 0.932876, 0.133611),
	float3( -0.924085, 0.201349, 0.133370),
	float3( 0.117467, -0.950371, 0.132619)
};
static const float PCF_SAMPLES_NUM = 7;

void spotlightFromProxy(float4 svPos : SV_Position, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{
#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface surface = read_gbuffer(svPos.xy);
#else
	SurfaceInterface surface = read_gbuffer(svPos.xy, sample_index);
#endif

	float3 L = Spotlight.position - surface.position;
	float distance = length(L);
	L = normalize(L);	

	float ld = saturate(-dot(L, Spotlight.direction));

	if(ld < Spotlight.apertureCos) {
		discard;
	}

	float4 smPos = mul(float4(surface.position, 1), Spotlight.shadowMatrix);
	smPos /= smPos.w;

	float3 N = surface.N;
	float3 V = surface.V;
	float3 H = normalize(V+L);

	float nl = saturate(dot(L, N));

	float falloff = lerp((ld - Spotlight.apertureCos) / (1 - Spotlight.apertureCos), 1.0f, 0);
	float attenuation = pow(saturate(1-distance/Spotlight.range), 2);	

	float3 mask = ReflectorMask.Sample(TextureSampler, smPos.xy);

	float shadow = 1;
	if(Spotlight.castsShadows) {
		smPos.z = min(smPos.z, 1 - pow(2, -20));
		shadow = 0;

		[unroll]
		for(int i=0 ; i<PCF_SAMPLES_NUM ; i++) {
			shadow += SpotlightShadowmap.SampleCmpLevelZero(ShadowmapSampler, smPos.xy + PCF_SHADOW_SAMPLES[i].xy / 512.f, smPos.z) * PCF_SHADOW_SAMPLES[i].z;
		}
	}

	float3 light_factor = shadow * falloff * attenuation * Spotlight.color * mask;
	output = light_factor * calculate_light(surface, L);
}

void pointlights_tiled(PostprocessVertex vertex, uint instance_id : SV_InstanceID, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{
	uint2 tileCoord = vertex.position.xy / 16;
	uint tileIndex = tileCoord.y * frame_.tiles_x + tileCoord.x;

	uint numLights = min(TileIndices[tileIndex], MAX_TILE_LIGHTS);

	//output = numLights / 64.f;
	//return;

#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(vertex.position.xy);
#else
	SurfaceInterface input = read_gbuffer(vertex.position.xy, sample_index);
#endif

	if(input.native_depth == DEPTH_CLEAR)
		discard;

	// in view space
	float3 N = mul(input.N, (float3x3) frame_.view_matrix);
	float3 V = input.VView;

	float3 acc = 0;

	[loop]
	for(uint i = 0; i < numLights; i++) {
		uint index = TileIndices[frame_.tiles_num + tileIndex * MAX_TILE_LIGHTS + i];
		PointLightData light = LightList[index];

		float3 L = light.positionView - input.positionView;
		float distance = length(L);

		float range = light.range;
		float radius = min(light.radius, range - 0.001f); 
		#ifdef FALOFF_TO_RADIUS
			radius = falloff_to_radius(light.radius, range);
		#endif

		// todo: move to cpu?
		float cutoff = 1 / pow((range - radius) / radius + 1, 2);

		float d = max(distance - radius, 0);
		float denom = d / radius + 1;
		float attenuation = 1 / (denom * denom);

		attenuation = (attenuation - cutoff) / (1-cutoff);
		attenuation = saturate(attenuation);

		L = normalize(L);
		float3 H = normalize(V+L);
		float nl = saturate(dot(L, N));
		//attenuation = pow(saturate(1-distance/light.range), 2);
		
		float3 light_factor = M_PI * attenuation * nl * light.color;
		acc += light_factor * material_radiance(input.albedo, input.f0, input.gloss, N, L, V, H);
	}

	output = acc;
	//output = lerp(float3(0,1,0), float3(1,0,0), (float)numLights / 4);
	//output = lerp(float3(0,1,0), float3(1,0,0), (float)numLights);
}

void directional_environment(PostprocessVertex vertex, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{

#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(vertex.position.xy);
	float shadow = ShadowsMainView[vertex.position.xy].x;
#else
	SurfaceInterface input = read_gbuffer(vertex.position.xy, sample_index);
	float shadow = calculate_shadow_fast(input.position, input.stencil);
#endif

	if(depth_not_background(input.native_depth)) {
		float ao = input.ao;

		float3 shaded = 0;

		shaded += input.base_color * input.emissive;
		shaded += main_directional_light(input) * shadow * sqrt(input.ao);
		shaded += ambient_specular(input.f0, input.gloss, input.N, input.V) * input.ao;
		shaded += ambient_diffuse(input.f0, input.gloss, input.N, input.V) * input.albedo * input.ao; // albedo has ao for "dirt"

		output = add_fog(shaded, input.depth, -input.V, get_camera_position());
	}
	else {
		output = add_fog(SkyboxColor(-input.V), input.depth, -input.V, get_camera_position());
	}
}
