// @define SAMPLE_FREQ_PASS

#include "Light.hlsli"

StructuredBuffer<PointLightData> LightList : register (t13);
StructuredBuffer<uint> TileIndices : register(t14);

void __pixel_shader(PostprocessVertex vertex, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{
	uint2 tileCoord = vertex.position.xy;
	tileCoord = tileCoord % frame_.Screen.resolution;
	tileCoord /= 16;
    uint tileIndex = mad(frame_.Screen.tiles_x, tileCoord.y, tileCoord.x);// tileCoord.y * frame_.Screen.tiles_x + tileCoord.x;

	uint numLights = min(TileIndices[tileIndex], MAX_TILE_LIGHTS);

#if !defined(MS_SAMPLE_COUNT) || defined(PIXEL_FREQ_PASS)
	SurfaceInterface input = read_gbuffer(vertex.position.xy);
#else
	SurfaceInterface input = read_gbuffer(vertex.position.xy, sample_index);
#endif

	if(input.native_depth == DEPTH_CLEAR)
		discard;

	// in view space
	float3 N = mul(input.N, (float3x3) frame_.Environment.view_matrix);
	float3 V = input.VView;

	float3 acc = 0;

	[loop]
	for(uint i = 0; i < numLights; i++) 
	{
        uint index = TileIndices[frame_.Screen.tiles_num + mad(MAX_TILE_LIGHTS, tileIndex, i)];
		PointLightData light = LightList[index];

		float3 L = light.positionView - input.positionView;
		float distance = length(L);
		L /= distance;
		float3 H = normalize(V + L);

		float range = light.range;
		float radius = min(light.radius, range - 0.001f); 
		#ifdef FALOFF_TO_RADIUS
			radius = falloff_to_radius(light.radius, range);
		#endif

		// todo: move to cpu?
        float radiusRcp = rcp(radius);
		float cutoff = pow(mad(range-radius, radiusRcp, 1.0f), -2);

		float d = max(distance - radius, 0);
        float denom = mad(d, radiusRcp, 1.0f);
		float attenuation = 1.0f / (denom * denom);

        attenuation = saturate((attenuation - cutoff) / (1 - cutoff));
		
        float ao = saturate(1 - (1 - input.ao) * frame_.Light.aoPointLight);
		float3 light_factor = attenuation * light.color * ao;
        acc += light_factor * MaterialRadiance(input.albedo, input.f0, input.gloss * light.glossFactor, N, L, V, H, light.diffuseFactor);
	}

	output = acc;
}
