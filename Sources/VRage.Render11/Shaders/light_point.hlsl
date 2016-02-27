// @define SAMPLE_FREQ_PASS

#include <Lighting/light.h>

void __pixel_shader(PostprocessVertex vertex, out float3 output : SV_Target0
#ifdef SAMPLE_FREQ_PASS
	, uint sample_index : SV_SampleIndex
#endif
	)
{
    uint2 tileCoord = vertex.position.xy / 16;
    uint tileIndex = mad(frame_.tiles_x, tileCoord.y, tileCoord.x);// tileCoord.y * frame_.tiles_x + tileCoord.x;

	uint numLights = min(TileIndices[tileIndex], MAX_TILE_LIGHTS);

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
        uint index = TileIndices[frame_.tiles_num + mad(MAX_TILE_LIGHTS, tileIndex, i)];
		PointLightData light = LightList[index];

		float3 L = light.positionView - input.positionView;
		float distance = length(L);

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

		L = normalize(L);
		float3 H = normalize(V+L);
		float nl = saturate(dot(L, N));
		
		float3 light_factor = M_PI * attenuation * nl * light.color;
		acc += light_factor * MaterialRadiance(input.albedo, input.f0, input.gloss, N, L, V, H);
	}

	output = acc;
}
