#ifndef GBUFFER_WRITE_H__
#define GBUFFER_WRITE_H__

struct GbufferOutput 
{
    float4 gbuffer0 : SV_Target0;
    float4 gbuffer1 : SV_Target1;
    float4 gbuffer2 : SV_Target2;
};

void gbuffer_write(out GbufferOutput output, 
	float3 color, float metal, float gloss, float3 N, uint id, float ao = 1, float emissive = 0, uint coverage = COVERAGE_MASK_ALL)
{
	output.gbuffer0.xyz = color;
    output.gbuffer0.w = metal;
    output.gbuffer1 = float4(N * 0.5 + 0.5, gloss);
    output.gbuffer2 = float4(ao, id / 255.f, emissive, coverage / 255.f);
}

#endif