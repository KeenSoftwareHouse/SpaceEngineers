#include <Common.hlsli>
#include <Postprocess/PostprocessBase.hlsli>

Texture2D	DepthResolved	: register( t5 );

#define NUM_SAMPLES 8
#define NUM_FOURS_BATCHES 2

#include <Frame.hlsli>
#include <Math/Math.hlsli>
#include <GBuffer/GBuffer.hlsli>

cbuffer SSAO_params : register( b1 )
{
	float min_radius;
	float max_radius;
	float radius_grow;
	float falloff;

	float radius_bias;
	float contrast;
	float normalization;
	float colorScale;

	float4 OcclPos[NUM_SAMPLES];
	float4 OcclPosFlipped[NUM_SAMPLES];
};

float2 view_to_screenspace(float3 pos) {
	float4 projected = mul(float4(pos, 1), frame_.Environment.projection_matrix);
	float2 clipspace = projected.xy / projected.w;
	clipspace.y *= -1;
	return clipspace * 0.5 + 0.5;
}

float ao_batch4(int i, float2 rot, float3 position, float3 T, float3 B, float radius, float falloff)
{	
		float2	roffs[4];			
		roffs[0]		= rot.yx * OcclPos[i].xy   + rot.xy * OcclPosFlipped[i].xy;
		roffs[1]		= rot.yx * OcclPos[i+1].xy + rot.xy * OcclPosFlipped[i+1].xy;
		roffs[2]		= rot.yx * OcclPos[i+2].xy + rot.xy * OcclPosFlipped[i+2].xy;
		roffs[3]		= rot.yx * OcclPos[i+3].xy + rot.xy * OcclPosFlipped[i+3].xy;
	
// construct world pos sample on plane
		float3	occlPos[4];		
		occlPos[0]		= position + roffs[0].x * T + roffs[0].y * B;
		occlPos[1]		= position + roffs[1].x * T + roffs[1].y * B;
		occlPos[2]		= position + roffs[2].x * T + roffs[2].y * B;
		occlPos[3]		= position + roffs[3].x * T + roffs[3].y * B;

// view space  version

		float2	occlProjPos[4];
		/*occlProjPos[0]	= occlPos[0].xy / occlPos[0].z;	
		occlProjPos[1]	= occlPos[1].xy / occlPos[1].z;	
		occlProjPos[2]	= occlPos[2].xy / occlPos[2].z;	
		occlProjPos[3]	= occlPos[3].xy / occlPos[3].z;	*/

		uint j;
		[unroll] for(j=0; j<4; j++) {
			occlProjPos[j] = view_to_screenspace(occlPos[j]);
		}

		   
// sample depth
		float4 zi;
		[unroll] for(j=0; j<4; j++) {
			// this complex calculation is placed here because of stero rendering
			// the calculation modifies screencord to proper place in gbuffer
			float2 screencoord = (frame_.Screen.offset_in_gbuffer + clamp(occlProjPos[j] * frame_.Screen.resolution, 0, frame_.Screen.resolution - 1)) / frame_.Screen.resolution_of_gbuffer;

			float nonlinearDepth = DepthResolved.SampleLevel(PointSampler, screencoord, 0).x;
			zi[j] = -linearize_depth(nonlinearDepth, frame_.Environment.projection_matrix);
		}
	
// compute ao portion of the sample	
		float4	zExtreme	= float4(OcclPos[i].w, OcclPos[i+1].w, OcclPos[i+2].w, OcclPos[i+3].w) * radius;	
//viewspace version
		float4	zmin		= -float4(occlPos[0].z, occlPos[1].z, occlPos[2].z, occlPos[3].z) - zExtreme;
	
		float4	D			= float4(zExtreme) * 2;	
		float4	dz			= min(max(zi - zmin,0), D);
	
// distant occluder attenuation
		float4 x			= saturate((zmin - zi) * falloff);
		float4 attDz		= x * x * D;

		float4 k			= step(zmin, zi);
		dz					= dz * k + (1-k) * attDz;

		return dot(1, dz);
}

float4 ao_taps4(uint2 pixel, int i, float2 rot, float3 position)
{	
	float3 T = float3(1,0,0);
	float3 B = float3(0,1,0);

	float2	roffs[4];			
	roffs[0]		= rot.yx * OcclPos[i].xy   + rot.xy * OcclPosFlipped[i].xy;
	roffs[1]		= rot.yx * OcclPos[i+1].xy + rot.xy * OcclPosFlipped[i+1].xy;
	roffs[2]		= rot.yx * OcclPos[i+2].xy + rot.xy * OcclPosFlipped[i+2].xy;
	roffs[3]		= rot.yx * OcclPos[i+3].xy + rot.xy * OcclPosFlipped[i+3].xy;
	
// construct world pos sample on plane
	float3	occlPos[4];		
	occlPos[0]		= position + roffs[0].x * T + roffs[0].y * B;
	occlPos[1]		= position + roffs[1].x * T + roffs[1].y * B;
	occlPos[2]		= position + roffs[2].x * T + roffs[2].y * B;
	occlPos[3]		= position + roffs[3].x * T + roffs[3].y * B;

// view space  version

	float2	occlProjPos[4];

	uint j;
	[unroll] for(j=0; j<4; j++) {
		occlProjPos[j] = view_to_screenspace(occlPos[j]);
	}

	[unroll] for(j=0; j<4; j++) {
		if( all(uint2(occlProjPos[j] * float2(1600, 900)) == pixel) ) {
			return 1;
		}
	}

	return 0;
}

//#define DISPLAY_TAPS

float4 __pixel_shader(PostprocessVertex input) : SV_Target0
{
#ifdef DISPLAY_TAPS
	uint2 testPixel = uint2(800, 800);

	if(all(uint2(input.position.xy) == testPixel)) {
		return float4(0,1,0,0);
	}

	SurfaceInterface gbuffer = read_gbuffer(testPixel);
	float depth = gbuffer.depth;

	float	radius	= min_radius + (1 - pow(abs(radius_grow), -depth)) * (max_radius - min_radius);
	float	bias	= radius * radius_bias;

	float3 N = gbuffer.N;
	N = normalize(mul(N, (float3x3)frame_.Environment.view_matrix));

	float2	rndrot;		
	float	noise	= dot(testPixel, float2(8.1,5.7));		
	sincos(noise, rndrot.x, rndrot.y);
	rndrot*= radius;	

    float3 view_pos = mad(0.5f, mad(bias, N, radius*N), gbuffer.positionView);

	float4 sum = 0;
	sum += ao_taps4(uint2(input.position.xy), 0, rndrot, view_pos);
	sum += ao_taps4(uint2(input.position.xy), 4, rndrot, view_pos);

	return sum;
#else
	
	SurfaceInterface gbuffer = read_gbuffer(input.position.xy);

	if(gbuffer.native_depth == DEPTH_CLEAR)
		discard;

	//float vignette = input.position - frame_.Screen.resolution;

	// unimportant calculation
/*	float margin = 1.f;
	float2 vignette_dist = saturate(input.position.xy / margin);
	vignette_dist = min(vignette_dist, saturate((frame_.Screen.resolution - input.position.xy - frame_.Screen.offset_in_gbuffer) / margin));
	float vignette = min(vignette_dist.x, vignette_dist.y);
	*/
	float depth = gbuffer.depth;

	float3 N = gbuffer.N;
	float3 T = float3(1,0,0);
	float3 B = float3(0,1,0);
	
// view space normal	
	N = normalize(mul(N, (float3x3)frame_.Environment.view_matrix));

    float	radius = lerp(min_radius, max_radius, 1 - pow(abs(radius_grow), -depth));
	float	bias	= radius * radius_bias;

	float2	rndrot;		
	float	noise	= dot(input.position.xy, float2(8.1,5.7));		
	sincos(noise, rndrot.x, rndrot.y);
	rndrot*= radius;	

    float3 view_pos = mad(0.5f, mad(bias, N, radius*N), gbuffer.positionView);

	float ao = 0;
    [unroll]
	for(int i=0; i< NUM_FOURS_BATCHES; i++)
		ao += ao_batch4(i * 4, rndrot, view_pos, T, B, radius, falloff );

	ao = saturate(ao * normalization / radius);
	ao = (ao - 0.5) * contrast + 0.5;
	
	return ao; //lerp(ao, 1, 1 - vignette);


#endif
}
