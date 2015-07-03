#ifndef MS_SAMPLE_COUNT
Texture2D<float>	DepthBuffer	: register( t0 );
Texture2D<float4>	Gbuffer0	: register( t1 );
Texture2D<float4>	Gbuffer1	: register( t2 );
Texture2D<float4>	Gbuffer2	: register( t3 );
Texture2D<uint2>	Stencil		: register( t4 );
#else
Texture2DMS<float, MS_SAMPLE_COUNT>		DepthBuffer	: register( t0 );
Texture2DMS<float4, MS_SAMPLE_COUNT>	Gbuffer0	: register( t1 );
Texture2DMS<float4, MS_SAMPLE_COUNT>	Gbuffer1	: register( t2 );
Texture2DMS<float4, MS_SAMPLE_COUNT>	Gbuffer2	: register( t3 );
Texture2DMS<uint2, MS_SAMPLE_COUNT>		Stencil		: register( t4 );
#endif

Texture2D<float> AOTexture : register( t12 );

// data read from indexed buffer instead of gbuffer
struct PerMaterialPayload {
	uint 	type; // 0 (default ggx), 1 (foliage normal-smoothing)
};

#include <frame.h>
#include <brdf.h>

StructuredBuffer<PerMaterialPayload> MaterialsSB : register( MERGE(t,MATERIAL_BUFFER_SLOT) );

bool coverage_all(uint coverage)
{
#ifndef MS_SAMPLE_COUNT
	return 1;
#elif MS_SAMPLE_COUNT == 2
	return coverage == 0x3;
#elif MS_SAMPLE_COUNT == 4
	return coverage == 0xF;
#elif MS_SAMPLE_COUNT == 8
	return coverage == 0xFF;
#endif
}

#include <Surface.h>

#include <math.h>

SurfaceInterface read_gbuffer(uint2 screencoord, uint sample = 0)
{
	SurfaceInterface gbuffer;

	float2 uv = screen_to_uv(screencoord);

	float hw_depth;
	float4 gbuffer0;
	float4 gbuffer1;
	float4 gbuffer2;
	uint stencil;
#ifndef MS_SAMPLE_COUNT
	hw_depth = DepthBuffer[screencoord].r;
	gbuffer0 = Gbuffer0[screencoord];
	gbuffer1 = Gbuffer1[screencoord];
	gbuffer2 = Gbuffer2[screencoord];
	stencil = Stencil[screencoord].g;
#else
	hw_depth = DepthBuffer.Load(screencoord, sample).r;
	gbuffer0 = Gbuffer0.Load(screencoord, sample);
	gbuffer1 = Gbuffer1.Load(screencoord, sample);
	gbuffer2 = Gbuffer2.Load(screencoord, sample);
	stencil = Stencil.Load(screencoord, sample).g;
#endif

	const float ray_x = 1./frame_.projection_matrix._11;
	const float ray_y = 1./frame_.projection_matrix._22;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, uv.x ), -lerp( -ray_y, ray_y, uv.y ), -1.);

	gbuffer.V = mul(screen_ray, transpose((float3x3)frame_.view_matrix));
	gbuffer.native_depth = hw_depth;
	gbuffer.depth = -linearize_depth(gbuffer.native_depth, frame_.projection_matrix);
	gbuffer.positionView = gbuffer.depth * screen_ray;
	gbuffer.position = get_camera_position() + gbuffer.depth * gbuffer.V;
	gbuffer.V = -normalize(gbuffer.V);
	gbuffer.VView = -normalize(screen_ray);

	gbuffer.base_color = gbuffer0.xyz;
	gbuffer.N = normalize(gbuffer1.xyz * 2 - 1);
	gbuffer.NView = mul(gbuffer.N, (float3x3)frame_.view_matrix);
	gbuffer.gloss = gbuffer1.w;

	gbuffer.metalness = gbuffer0.w;
	gbuffer.ao = min(gbuffer2.x, AOTexture.SampleLevel(LinearSampler, uv,0));
	gbuffer.id = gbuffer2.y * 255;
	gbuffer.coverage = gbuffer2.w * 255;
	gbuffer.emissive = gbuffer2.z;
	gbuffer.stencil = stencil;

	gbuffer.albedo = surface_albedo(gbuffer.base_color, gbuffer.metalness);
	gbuffer.f0 = surface_f0(gbuffer.base_color, gbuffer.metalness);

	gbuffer.material_type = MaterialsSB[gbuffer.id].type;

	return gbuffer;
}

static const float2 screen_offset_3x3[8] = 
{
	{-1,  1}, { 0,  1}, { 1,  1},
	{-1,  0}, 			{ 1,  0},
	{-1, -1}, { 0, -1}, { 1, -1},
};

// optimize with gathers?
bool gbuffer_edgedetect(uint2 screencoord)
{
	uint i;
	float depth = read_gbuffer(screencoord).native_depth;
	float4 depth1;
	float4 depth2;

	[unroll]
	for(i=0;i<4;i++)
	{
		depth1[i] = read_gbuffer(screencoord + screen_offset_3x3[i]).native_depth;
		depth2[i] = read_gbuffer(screencoord + screen_offset_3x3[4+i]).native_depth;
	}
	depth1 = abs(depth - depth1);
	depth2 = abs(depth - depth2);

	float4 maxd = max(depth1, depth2);
	float4 mind = max(min(depth1, depth2), 0.000001f);

	float4 depthr = step(mind * 100, maxd);

	float3 normal = read_gbuffer(screencoord).N;

	float4 normal1;
	float4 normal2;
	[unroll]
	for(i=0;i<4;i++)
	{
		normal1[i] = dot(normal, read_gbuffer(screencoord + screen_offset_3x3[i]).N);
		normal2[i] = dot(normal, read_gbuffer(screencoord + screen_offset_3x3[i+4]).N);
	}
	normal1 = abs(normal2-normal1);
	float4 normalr = step(0.3, normal1);
	normalr = max(normalr, depthr);

	//return dot(normalr, 0.25) * (!coverage_all(read_gbuffer(screencoord).coverage));

	float depthMin = depth;
	float depthMax = depth;
	uint coverage = read_gbuffer(screencoord).coverage;
	float3 N = read_gbuffer(screencoord).N;
	float3 N1 = N;
#ifdef MS_SAMPLE_COUNT
	[unroll]
	for(i=1; i<MS_SAMPLE_COUNT; i++) {

		if(coverage & (1 << i)) {
			depthMin = min(depthMin, read_gbuffer(screencoord, i).native_depth);
			depthMax = max(depthMax, read_gbuffer(screencoord, i).native_depth);

			float3 sampleN = read_gbuffer(screencoord, i).N;
			if(dot(sampleN, N) < dot(N, N1)) {
				N1 = sampleN;
			}
		}
	}
#endif
	bool covered = coverage_all(coverage) || (depthMax == 0);
	
	return !covered && dot(normalr, 0.25);

	//return !coverage_all(read_gbuffer(screencoord).coverage) && (depthMax > 0);
}