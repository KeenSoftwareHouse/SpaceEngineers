#ifndef GBUFFER_WRITE_H__
#define GBUFFER_WRITE_H__

#include <frame.h>
#include <vertex_transformations.h>

struct GbufferOutput 
{
    float4 gbuffer0 : SV_Target0;
    float4 gbuffer1 : SV_Target1;
    float4 gbuffer2 : SV_Target2;
#ifdef CUSTOM_DEPTH
	float depth : SV_Depth;
#endif
};

void gbuffer_write(out GbufferOutput output, 
	float3 color, float metal, float gloss, float3 N, uint id, float ao = 1, float emissive = 0, uint coverage = COVERAGE_MASK_ALL
#ifdef CUSTOM_DEPTH
	, float depth = 0
#endif
	)
{
    float3 nview = normalize(world_to_view(N));
    float2 nenc = pack_normals2(nview);
	output.gbuffer0 = float4(color, id / 255.f);
    output.gbuffer1 = float4(nenc, ao, 0);
    output.gbuffer2 = float4(metal, gloss, emissive, coverage / 255.f);

#ifdef CUSTOM_DEPTH
	output.depth = depth;
#endif
}

#endif