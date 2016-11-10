// @define SAMPLE_FREQ_PASS

#include "Light.hlsli"

cbuffer Spotlights : register(b1)
{
    SpotlightConstants Spotlight;
}

void __vertex_shader(float4 vertexPos : POSITION, out float4 svPos : SV_Position)
{
	float4 pos = unpack_position_and_scale(vertexPos);
	pos.xyz *= 2;
	svPos = mul(pos, Spotlight.worldViewProj);
}

Texture2D<float3> ReflectorMask : register(t13);
Texture2D<float> SpotlightShadowmap : register(t14);

static const float3 PCF_SHADOW_SAMPLES[] = 
{
	float3( -0.024989, 0.005582, 0.182518),
	float3( 0.677835, -0.380678, 0.143624),
	float3( 0.599357, 0.590492, 0.139817),
	float3( -0.694194, -0.617016, 0.134441),
	float3( -0.130402, 0.932876, 0.133611),
	float3( -0.924085, 0.201349, 0.133370),
	float3( 0.117467, -0.950371, 0.132619)
};
static const float PCF_SAMPLES_NUM = 7;

void __pixel_shader(float4 svPos : SV_Position, out float3 output : SV_Target0
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

	float3 surfaceToLight = Spotlight.position - surface.position;
    float distance = length(surfaceToLight);
    float3 L = surfaceToLight / distance;

    float ldac = saturate(-dot(L, Spotlight.direction)) - Spotlight.apertureCos;

    clip(ldac);

    float4 smPos = mul(float4(surface.position - frame_.Environment.eye_offset_in_world, 1), Spotlight.shadowMatrix);
    smPos /= smPos.w;
    float3 mask = ReflectorMask.Sample(TextureSampler, smPos.xy);

	float3 N = surface.N;
	float3 V = surface.V;
	float3 H = normalize(V+L);

	float nl = dot(L, N);

    float falloff = 1 - pow(saturate(1 - ldac / (1 - Spotlight.apertureCos)), 4);

    // attenuation
    float scalarDistanceToSurface = dot(surfaceToLight, -Spotlight.direction);
    float attenuation = pow(saturate(1 - scalarDistanceToSurface / Spotlight.range), 1.7);

	float shadow = 1;
	if(Spotlight.castsShadows > 0) 
	{
		smPos.z = min(smPos.z, 1 - pow(2, -20));
		shadow = 0;

		[unroll]
		for(int i=0 ; i<PCF_SAMPLES_NUM ; i++) 
		{
			shadow += SpotlightShadowmap.SampleCmpLevelZero(ShadowmapSampler, smPos.xy + PCF_SHADOW_SAMPLES[i].xy / 512.f, smPos.z) * PCF_SHADOW_SAMPLES[i].z;
		}

        shadow = 1 - (1 - shadow) * (1 - frame_.Light.shadowFadeout);
	}

    float ao = saturate(1 - (1 - surface.ao) * frame_.Light.aoSpotLight);
    float3 light_factor = falloff * attenuation * mask * shadow * ao;
    output = light_factor * calculate_light(surface, L, Spotlight.color, Spotlight.glossFactor, Spotlight.diffuseFactor);
}
