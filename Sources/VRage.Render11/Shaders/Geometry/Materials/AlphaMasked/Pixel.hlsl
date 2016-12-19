#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Math/Color.hlsli>
#include <Geometry/AlphamaskViews.hlsli>

float2 transformUV(int index, float3 inCDir)
{
	float3x3 m = (float3x3)alphamask_constants.impostor_view_matrix[index];
	float3 trTex = mul(inCDir, m);
	trTex = (trTex / 2.0 + 0.5);
	trTex.y = 1 - trTex.y;
	return trTex.xy;
}

#define TREE_SCALE 20

float4 sampleColor(float3 cDir, float2 uv, int index, int subIndex = 0)
{
	uint NT = 181;

    uv = transformUV(index, cDir / TREE_SCALE);
	float3 tex = float3(uv, (index * 2 + subIndex));
	return AlphaMaskArrayTexture.Sample(AlphamaskArraySampler, tex);
}

float4 sampleTree(float3 cDir, float2 uv, int index)
{
	return sampleColor(cDir, uv, index, 1);
}

#define THREE_SAMPLES 1
#define ALTER_DEPTH 1

float4 FetchImpostorExtras(PixelInterface pixel)
{
	float4 t1 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.x);
#ifdef THREE_SAMPLES
	float4 t2 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.y);
	float4 t3 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.z);

    float4 tIt = (t1 * pixel.custom.view_blends.x + t2 * pixel.custom.view_blends.y + t3 * pixel.custom.view_blends.z);
 #else
    float4 tIt = t1;
 #endif
	return float4(tIt.z, 0, tIt.w, tIt.r);
}

float3 FetchImpostorNormal(PixelInterface pixel, float mainDepth)
{
    const float delta = 1.0f / 255.0f;
	float tx1 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0 + float2(-delta, 0), pixel.custom.view_indices.x).r;
	float tx2 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0 + float2(delta, 0), pixel.custom.view_indices.x).r;
	float ty1 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0 + float2(0, -delta), pixel.custom.view_indices.x).r;
	float ty2 = sampleTree(pixel.custom.cDir, pixel.custom.texcoord0 + float2(0, delta), pixel.custom.view_indices.x).r;
    float2 xy = float2(((mainDepth - tx1) + (tx2 - mainDepth)) / 2, ((mainDepth - ty1) + (ty2 - mainDepth)) / 2);
    float z = 0.1;
    
	return float3(-xy, z * sign(0.5f - mainDepth));
}

float4 FetchImpostorCM(PixelInterface pixel, float4 extras)
{
	float4 cm1 = sampleColor(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.x);
#ifdef THREE_SAMPLES
	float4 cm2 = sampleColor(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.y);
	float4 cm3 = sampleColor(pixel.custom.cDir, pixel.custom.texcoord0, pixel.custom.view_indices.z);
	return (cm1 * pixel.custom.view_blends.x + cm2 * pixel.custom.view_blends.y + cm3 * pixel.custom.view_blends.z);
#else
    return cm1;
#endif
}

void RenderArray(PixelInterface pixel, inout MaterialOutputInterface output)
{
	float4 extras = FetchImpostorExtras(pixel);
	float alpha = extras.z;
	clip(alpha - 0.2);

#ifdef ALTER_DEPTH
	output.depth = OffsetDepth(pixel.screen_position.z, frame_.Environment.projection_matrix, 40 * extras.w);
#endif

#ifndef DEPTH_ONLY
	if (alpha >= 0.2)
	{
	    float3 normal = FetchImpostorNormal(pixel, extras.w);
		float4 cm = FetchImpostorCM(pixel, extras);
        float fullRange = pixel.custom.texcoord0.x * 2 - 1;
        float3x3 rmat_x = rotate_y(-(M_PI * pixel.custom.texcoord0.x - M_PI_2));
        float3x3 rmat_y = rotate_x(-(M_PI * pixel.custom.texcoord0.y - M_PI_2));
        normal = mul(mul(rmat_x, rmat_y), normalize(normal));
        float4 ng = float4((normalize(normal) + 1) / 2, 0);

        cm.w = 0;
        FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
	}
#endif
}

void RenderSingle(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifdef USE_TEXTURE_INDICES
	output.coverage = AlphamaskCoverageAndClip(0.5f, pixel.custom.texcoord0, pixel.custom.texIndices.w);
#else
	output.coverage = AlphamaskCoverageAndClip(0.5f, pixel.custom.texcoord0, 0);
#endif

#ifndef DEPTH_ONLY
#ifdef USE_TEXTURE_INDICES
	float4 texIndices = pixel.custom.texIndices;
	float4 cm = ColorMetalArrayTexture.Sample(TextureSampler, float3(pixel.custom.texcoord0, texIndices.x));
	float4 ng = NormalGlossArrayTexture.Sample(TextureSampler, float3(pixel.custom.texcoord0, texIndices.y));
	float4 extras = ExtensionsArrayTexture.Sample(TextureSampler, float3(pixel.custom.texcoord0, texIndices.z));
#else
	float4 cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 extras = ExtensionsTexture.Sample(TextureSampler, pixel.custom.texcoord0);
#endif
#ifdef BUILD_TANGENT_IN_PIXEL
    FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, pixel.custom.normal, output, ng, cm, extras);
#else
	FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
#endif
#endif
}

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#if defined(DITHERED_LOD)
	// discards pixels
	Dither(pixel.screen_position, pixel.custom_alpha);
#endif

#ifdef ALPHA_MASK_ARRAY

    RenderArray(pixel, output);

#else
    RenderSingle(pixel, output);
#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
