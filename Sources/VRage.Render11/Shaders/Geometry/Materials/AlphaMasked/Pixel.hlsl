#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>
#include <Math/Color.hlsli>
#include <Geometry/AlphamaskViews.hlsli>

float4 sampleColor(float2 uv, int index, int subIndex = 0, float2 uvDelta = float2(0,0))
{
	uint NT = 181;

	float3 tex = float3(uv, (index * 2 + subIndex));
	return AlphaMaskArrayTexture.Sample(AlphamaskArraySampler, tex);
}

float4 sampleTree(float2 uv, int index, float2 uvDelta = float2(0,0))
{
	return sampleColor(uv, index, 1, uvDelta);
}

#define TREE_SCALE 20

#define THREE_SAMPLES 1
#define ALTER_DEPTH 1

float4 FetchImpostorExtras(PixelInterface pixel)
{
	float4 t1 = sampleTree(pixel.custom.texcoord0, pixel.custom.view_indices.x);
#ifdef THREE_SAMPLES
	float4 t2 = sampleTree(pixel.custom.texcoord0, pixel.custom.view_indices.y);
	float4 t3 = sampleTree(pixel.custom.texcoord0, pixel.custom.view_indices.z);

    float4 tIt = (t1 * pixel.custom.view_blends.x + t2 * pixel.custom.view_blends.y + t3 * pixel.custom.view_blends.z);
 #else
    float4 tIt = t1;
 #endif
	return float4(tIt.z, 0, tIt.w, tIt.r);
}

float3 FetchImpostorNormal(PixelInterface pixel, float mainDepth)
{
    const float delta = 1.0f / 255.0f;
	float tx1 = sampleTree(pixel.custom.texcoord0 + float2(-delta, 0), pixel.custom.view_indices.x).r;
	float tx2 = sampleTree(pixel.custom.texcoord0 + float2(delta, 0), pixel.custom.view_indices.x).r;
	float ty1 = sampleTree(pixel.custom.texcoord0 + float2(0, -delta), pixel.custom.view_indices.x).r;
	float ty2 = sampleTree(pixel.custom.texcoord0 + float2(0, delta), pixel.custom.view_indices.x).r;
    float2 xy = float2(((mainDepth - tx1) + (tx2 - mainDepth)) / 2, ((mainDepth - ty1) + (ty2 - mainDepth)) / 2);
    float z = 0.1;
    
	return float3(-xy, z * sign(0.5f - mainDepth));
}

float4 FetchImpostorCM(PixelInterface pixel, float4 extras)
{
	float4 cm1 = sampleColor(pixel.custom.texcoord0, pixel.custom.view_indices.x);
#ifdef THREE_SAMPLES
	float4 cm2 = sampleColor(pixel.custom.texcoord0, pixel.custom.view_indices.y);
	float4 cm3 = sampleColor(pixel.custom.texcoord0, pixel.custom.view_indices.z);
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
	output.depth = OffsetDepth(pixel.screen_position.z, frame_.projection_matrix, 40 * extras.w);
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
	output.coverage = AlphamaskCoverageAndClip(0.5f, pixel.custom.texcoord0);

#ifndef DEPTH_ONLY
	float4 cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
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
