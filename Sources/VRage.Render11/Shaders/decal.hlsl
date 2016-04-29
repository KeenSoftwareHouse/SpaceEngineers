// @define USE_NORMALMAP_DECAL
// @define USE_COLORMAP_DECAL

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

struct DecalConstants
{
	matrix WorldMatrix;     // Position of unit box decal volume
	matrix InvWorldMatrix;
};

cbuffer DecalConstants : register ( b2 )
{
	DecalConstants Decals[512];
};

// textures
Texture2D<float> DecalAlpha : register( t3 );
Texture2D<float4> DecalColor : register( t4 );
Texture2D<float4> DecalNormalmap : register( t5 );

struct VsOut
{
	float4 position : SV_Position;
	float3 normal : NORMAL;
	uint id : DECALID;
};

VsOut __vertex_shader(uint vertex_id : SV_VertexID)
{
	uint vId = vertex_id % 8;
	uint decalId = vertex_id / 8;
	uint quadId = vId % 4;
	float3 vertexPosition;
	vertexPosition.x = -0.5f + (quadId / 2);
	vertexPosition.y = -0.5f + (quadId == 0 || quadId == 3);
	vertexPosition.z = -0.5f + (vId / 4);

    matrix worldMatrix = Decals[decalId].WorldMatrix;
	float4 wposition = mul(float4(vertexPosition.xyz, 1), worldMatrix);

	VsOut result;
	result.position = mul(wposition, frame_.view_projection_matrix);
	result.normal = normalize(mul(float3(0,0,-1), (float3x3)worldMatrix));
	result.id = decalId;
	return result;
}

float3x3 pixel_tangent_space(float3 N, float3 pos, float2 uv) {
	float3 dp1 =  ddx(pos);
	float3 dp2 =  ddy(pos);
	float2 duv1 =  ddx(uv);
	float2 duv2 =  ddy(uv);
	float3 dp2perp = cross( dp2, N );
    float3 dp1perp = cross( N, dp1 );

    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = rsqrt( max( dot(T,T), dot(B,B) ) );
    return float3x3( T * invmax, B * invmax, N );
}

// copy of gbuffer normals? (resolved for msaa...)
void __pixel_shader(VsOut vertex, out float4 out_gbuffer0 : SV_TARGET0
#ifdef USE_DUAL_SOURCE_BLENDING
    , out float4 out_alpha : SV_TARGET1
#else
    , out float4 out_gbuffer1 : SV_TARGET1, out float4 out_gbuffer2 : SV_TARGET2
#endif
)
{
    out_gbuffer0 = 0;
	float2 screencoord = vertex.position.xy;

	float hw_depth;
	float4 gbuffer1;

#ifndef MS_SAMPLE_COUNT
	hw_depth = DepthBuffer[screencoord].r;
	gbuffer1 = Gbuffer1[screencoord];
#else
	hw_depth = DepthBuffer.Load(screencoord, 0).r;
	gbuffer1 = Gbuffer1.Load(screencoord, 0);
#endif

	float2 uv = screen_to_uv(screencoord);
	const float ray_x = 1./frame_.projection_matrix._11;
	const float ray_y = 1./frame_.projection_matrix._22;
	float3 screen_ray = float3(lerp( -ray_x, ray_x, uv.x ), -lerp( -ray_y, ray_y, uv.y ), -1.);

	float3 V = mul(screen_ray, transpose((float3x3)frame_.view_matrix));
	float depth = -linearize_depth(hw_depth, frame_.projection_matrix);
	float3 position = get_camera_position() + depth * V;

	float4 decalPos = mul(float4(position, 1), Decals[vertex.id].InvWorldMatrix);
	decalPos /= decalPos.w;

	float3 gbufferN = normalize(gbuffer1.xyz * 2 - 1);
	float3 projN = normalize(vertex.normal);

    float2 texcoord = decalPos.xy * -1 + 0.5;
    float decalAlpha = DecalAlpha.Sample(TextureSampler, texcoord);

    if (decalAlpha == 0 || any(abs(decalPos.xyz) > 0.5) || dot(projN, gbufferN) < 0.33)
        discard;

#ifdef USE_COLORMAP_DECAL
    float4 decalCm = DecalColor.Sample(TextureSampler, texcoord);

#ifdef USE_DUAL_SOURCE_BLENDING
    out_alpha = decalAlpha;
    out_gbuffer0 = decalCm;
#else
    // In this case we basically ignore metal
    out_gbuffer0 = float4(decalCm.rgb, decalAlpha);
#endif

#endif

#ifdef USE_NORMALMAP_DECAL
    float4 decalNmSample = DecalNormalmap.Sample(TextureSampler, texcoord);
    float3 decalNm = decalNmSample.xyz * 2 - 1;
    float3x3 tangent_to_world = pixel_tangent_space(gbufferN, position, texcoord);
    float3 blendedN = normalize(mul(decalNm, tangent_to_world));
    float aoBump = pow(dot(blendedN, gbufferN), 4);

    float3 outN = blendedN * 0.5 + 0.5;
    float gloss = decalNmSample.a;

    // NOTE: Relies on alpha mask discard above. If we try to do blending
    // here it can't work because it would reuse same Gbuffer1 copy each
    // time, getting bad artifacts
    out_gbuffer1 = float4(outN, gloss);
    out_gbuffer2 = aoBump;
#endif
}