#include <common.h>
#include <frame.h>

#ifndef MS_SAMPLE_COUNT
Texture2D<float>	DepthBuffer	: register( t0 );
Texture2D<float4>	Gbuffer1	: register( t1 );
Texture2D<uint2>	Stencil		: register( t2);
#else
Texture2DMS<float, MS_SAMPLE_COUNT>		DepthBuffer	: register( t0 );
Texture2DMS<float4, MS_SAMPLE_COUNT>	Gbuffer1	: register( t1 );
Texture2DMS<uint2, MS_SAMPLE_COUNT>		Stencil		: register( t2 );
#endif

#include <gbuffer_write.h>

cbuffer Decal : register ( b2 )
{
	matrix WorldMatrix;
	matrix InvWorldMatrix;
};

// textures
Texture2D<float> DecalAlpha;

struct VsIn
{
	float3 position : POSITION;
};

struct VsOut
{
	float4 position : SV_Position;
};


VsOut vs(VsIn vertex, uint vertex_id : SV_VertexID)
{
	VsOut result;
	result.position = mul(float4(vertex.position.xyz, 1), frame_.view_projection_matrix);
	return result;
}

// copy of gbuffer normals? (resolved for msaa...)
float4 ps(VsOut vertex, GbufferOutput gbuffer)
{
	float2 screencoord = vertex.position.xy;

	float hw_depth;
	float gbuffer1;

#ifndef MS_SAMPLE_COUNT
	hw_depth = DepthBuffer[screencoord].r;
	gbuffer1 = Gbuffer1[screencoord];
#else
	hw_depth = DepthBuffer.Load(screencoord, sample).r;
	gbuffer1 = Gbuffer1.Load(screencoord, sample);
#endif

	const float ray_x = 1./frame_.projection_matrix._11;
	const float ray_y = 1./frame_.projection_matrix._22;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, uv.x ), -lerp( -ray_y, ray_y, uv.y ), -1.);

	float3 V = mul(screen_ray, transpose((float3x3)frame_.view_matrix));
	float depth = -linearize_depth(hw_depth, frame_.projection_matrix);
	float3 position = get_camera_position() + depth * screen_ray * V;

	float3 decalPos = mul(float4(position, 1), InvWorldMatrix).xyz;
	float2 texcoord = decalPos.xz * 0.5 + 0.5;

	float alpha = DecalAlpha.Sample(TextureSampler, texcoord);

	if(any(decalPos) > 0.5f || alpha < 0.5f) {
		discard;
	}

	float3 N = normalize(gbuffer1.xyz * 2 - 1);
	float g = gbuffer1.w;

	flaot3 decalN = float3(0,0,1);
	// decal to world space normal

	// combine normalmaps

	gbuffer_write(gbuffer, color, metal, g, N, _, ao, emissive, _ );
}

// blend modes
// gbuffer 0 (color metal) ->
// 		overwrite
// gbuffer 1 (normal gloss) ->
//		overwrite
// gbuffer 2 (ao, id, emissive, coverage) ->
// 		overwrite ao, _, emissive, _ (mask)