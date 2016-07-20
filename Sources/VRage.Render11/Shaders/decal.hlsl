// @define USE_COLORMAP_DECAL, USE_NORMALMAP_DECAL, USE_EXTENSIONS_TEXTURE, RENDER_TO_TRANSPARENT

#define OIT

#include <common.h>
#include <frame.h>
#include <Transparency/Globals.h>
#include <vertex_transformations.h>
#include <pixel_utils.h>

#define AO_BUMP_POW 4
#define NORMAL_DOT_FACTOR 4

#ifndef MS_SAMPLE_COUNT
Texture2D<float>	DepthBuffer	: register( t0 );
Texture2D<float4>	Gbuffer1Copy	: register( t1 );
#else
Texture2DMS<float, MS_SAMPLE_COUNT>		DepthBuffer	: register( t0 );
Texture2DMS<float4, MS_SAMPLE_COUNT>	Gbuffer1Copy	: register(t1);
#endif

struct DecalConstants
{
    float4 WorldMatrixRow0;
    float4 WorldMatrixRow1;
    float4 WorldMatrixRow2;
    float FadeAlpha;
    float _p1, _p2, _p3;
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
Texture2D<float4> DecalExtensions : register(t6);

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

    matrix worldMatrix = transpose(matrix(
        Decals[decalId].WorldMatrixRow0,
        Decals[decalId].WorldMatrixRow1,
        Decals[decalId].WorldMatrixRow2,
        float4(0, 0, 0, 1)));
	float4 wposition = mul(float4(vertexPosition.xyz, 1), worldMatrix);

	VsOut result;
	result.position = mul(wposition, frame_.view_projection_matrix);
	result.normal = normalize(mul(float3(0,0,-1), (float3x3)worldMatrix));
	result.id = decalId;
	return result;
}

#ifdef RENDER_TO_TRANSPARENT
void __pixel_shader(VsOut vertex, out float4 accumTarget : SV_TARGET0, out float4 coverageTarget : SV_TARGET1)
{
#else
void __pixel_shader(VsOut vertex, out float4 out_gbuffer2 : SV_TARGET2

#ifdef USE_COLORMAP_DECAL
    , out float4 out_gbuffer0 : SV_TARGET0
#endif

#ifdef USE_NORMALMAP_DECAL
    , out float4 out_gbuffer1 : SV_TARGET1
#endif
    )
{
#endif
	float hw_depth;
	float4 gbuffer1;

	float2 screencoord = vertex.position.xy;
#ifndef MS_SAMPLE_COUNT
	hw_depth = DepthBuffer[screencoord].r;
	gbuffer1 = Gbuffer1Copy[screencoord];
#else
	hw_depth = DepthBuffer.Load(screencoord, 0).r;
	gbuffer1 = Gbuffer1Copy.Load(screencoord, 0);
#endif
	screencoord -= frame_.offset_in_gbuffer;

	float2 uv = (screencoord) / frame_.resolution;

    float3 screen_ray = compute_screen_ray(uv);
    float3 Vinv = view_to_world(screen_ray);
    float depth = compute_depth(hw_depth);
    float3 position = Vinv * depth - frame_.eye_offset_in_world;

	float4 decalPos = mul(float4(position, 1), Decals[vertex.id].InvWorldMatrix);
	decalPos /= decalPos.w;

    float3 gbufferNView = unpack_normals2(gbuffer1.xy);
    float3 gbufferN = view_to_world(gbufferNView);
    float3 projN = normalize(vertex.normal);

    float fadeAlpha = Decals[vertex.id].FadeAlpha;

	float2 texcoord = decalPos.xy * -1 + 0.5;
    float decalAlpha = DecalAlpha.Sample(TextureSampler, texcoord);

	if (any(abs(decalPos.xyz) > 0.5))
		discard;

	if (abs(dot(projN, gbufferN)) < 0.33)
		discard;

    float metal = 0;
    float gloss = 0;
    float emissive = 0;

#ifdef USE_EXTENSIONS_TEXTURE
    float4 extensions = DecalExtensions.Sample(TextureSampler, texcoord);
    emissive = extensions.g;
#endif

#ifdef USE_COLORMAP_DECAL
    float4 decalCm = DecalColor.Sample(TextureSampler, texcoord);
    float4 decalCmRGBA = float4(decalCm.rgb, decalAlpha) * fadeAlpha;
#ifdef RENDER_TO_TRANSPARENT
    TransparentColorOutput(decalCmRGBA, depth, vertex.position.z, 1.0f, accumTarget, coverageTarget);
#else
    metal = decalCm.a;
    out_gbuffer0 = decalCmRGBA;
#endif
#endif

#ifdef USE_NORMALMAP_DECAL
    float4 decalNmSample = DecalNormalmap.Sample(TextureSampler, texcoord);
    float3 decalNm = normalize(decalNmSample.xyz * 2 - 1); // Source may be filtered
    
    adjust_normalmap_no_precomputed_tangents(decalNm);

    // Find distance from basic normal and multiply by a grow factor
    float alpha1 = saturate((1 - dot(decalNm, float3(0, 0, 1))) * NORMAL_DOT_FACTOR);
    float normalAlpha = max(decalAlpha, alpha1);

    float3x3 tangent_to_world = PixelTangentSpace(gbufferN, position, texcoord);
    float3 blendedN = normalize(mul(decalNm, tangent_to_world));

#ifdef USE_EXTENSIONS_TEXTURE
    float aoBump = extensions.r;
#else
    float aoBump = pow(dot(blendedN, gbufferN), AO_BUMP_POW);
#endif

    gloss = decalNmSample.a;

    float3 blendedNView = world_to_view(blendedN);
    float2 enc = pack_normals2(blendedNView);

    // Don't multiply normals and ao because they are already multiplied by the blendstate
    out_gbuffer1 = float4(enc, aoBump, normalAlpha * fadeAlpha);
#endif

#ifndef RENDER_TO_TRANSPARENT
    out_gbuffer2 = float4(metal, gloss, emissive, decalAlpha) * fadeAlpha;
#endif
}