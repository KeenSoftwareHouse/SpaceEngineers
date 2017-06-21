// @define USE_COLORMETAL_TEXTURE, USE_NORMALGLOSS_TEXTURE, USE_EXTENSIONS_TEXTURE, RENDER_TO_TRANSPARENT

#ifdef RENDER_TO_TRANSPARENT
#define OIT
#include <Transparent/OIT/Globals.hlsli>
#endif

#include <Common.hlsli>
#include <Frame.hlsli>
#include <VertexTransformations.hlsli>
#include <PixelUtils.hlsli>
#include <GBuffer/GBufferWrite.hlsli>
#include "DecalsCommon.hlsli"

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
	result.position = mul(wposition, frame_.Environment.view_projection_matrix);
	result.normal = normalize(mul(float3(0,0,-1), (float3x3)worldMatrix));
	result.id = decalId;
	return result;
}

// decalPosV: decal pixel position in view coordinates
// decalPosL: decal pixel position in decal coordinates
void GetDecalPosition(uint decalId, float2 screencoord, float depth, out float3 decalPosV, out float4 decalPosL)
{
    screencoord -= frame_.Screen.offset_in_gbuffer;
    float2 uv = screencoord / frame_.Screen.resolution;

    float3 screen_ray = compute_screen_ray(uv);
    float3 Vinv = view_to_world(screen_ray);
    decalPosV = Vinv * depth - frame_.Environment.eye_offset_in_world;

    decalPosL = mul(float4(decalPosV, 1), Decals[decalId].InvWorldMatrix);
    decalPosL /= decalPosL.w;
}

#ifdef RENDER_TO_TRANSPARENT
void __pixel_shader(VsOut vertex, out float4 accumTarget : SV_TARGET0, out float4 coverageTarget : SV_TARGET1)
{
#else
void __pixel_shader(VsOut vertex, out GbufferOutput output)
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

    float depth = compute_depth(hw_depth);

    float3 decalPosV;
    float4 decalPosL;
    GetDecalPosition(vertex.id, screencoord, depth, decalPosV, decalPosL);

    float3 gbufferNView = unpack_normals2(gbuffer1.xy);
    float3 gbufferN = view_to_world(gbufferNView);
    float3 decalN = normalize(vertex.normal);

    if (any(abs(decalPosL.xyz) > 0.5) || abs(dot(decalN, gbufferN)) < 0.33)
        discard;

    float fadeAlpha = Decals[vertex.id].FadeAlpha;
    float2 texcoord = decalPosL.xy * -1 + 0.5;

#ifdef RENDER_TO_TRANSPARENT
    float3 color;
    float metal;
    ReadColorMetalTexture(texcoord, color, metal);
    float decalAlpha = ReadAlphamaskTexture(texcoord);
    float4 decalCmRGBA = float4(color, decalAlpha) * fadeAlpha;
    TransparentColorOutput(decalCmRGBA, depth, vertex.position.z, 1.0f, accumTarget, coverageTarget);
#else
    float decalAlpha = ReadAlphamaskTexture(texcoord);

#ifdef USE_EXTENSIONS_TEXTURE
    float ao;
    float emissive;
    ReadExtensionsTexture(texcoord, ao, emissive);
#else
    float ao = 0;
    float emissive = 0;
#endif

#ifdef USE_COLORMETAL_TEXTURE
    float3 color;
    float metal;
    ReadColorMetalTexture(texcoord, color, metal);
#else
    float3 color = float3(0, 0, 0);
    float metal = 0;
#endif

#ifdef USE_NORMALGLOSS_TEXTURE
    float gloss;
    float3 decalNm;
    ReadNormalGlossTexture(texcoord, decalNm, gloss);

    adjust_normalmap_no_precomputed_tangents(decalNm);

    float3x3 tangent_to_world = PixelTangentSpace(gbufferN, decalPosV, texcoord);
    float3 blendedN = normalize(mul(decalNm, tangent_to_world));

#ifndef USE_EXTENSIONS_TEXTURE
    ao = ComputeAmbientOcclusionBestEffort(gbufferN, blendedN);
#endif
    float normalAlpha = ComputeNormalMapAlphaBestEffort(decalNm, decalAlpha);
#else
    float3 blendedN = float3(0, 0, 0);
    float gloss = 0;
    float normalAlpha = 0;
#endif

    GbufferWriteBlend(output, color, metal, blendedN, gloss, ao, emissive, decalAlpha, normalAlpha, fadeAlpha);
#endif
}
