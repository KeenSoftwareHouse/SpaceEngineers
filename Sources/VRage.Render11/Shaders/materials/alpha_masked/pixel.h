#include <Math/Color.h>
#include <alphamaskViews.h>

float2 transformUV(int index, float3 inCDir)
{
	float3x3 m = (float3x3)alphamask_constants.impostor_view_matrix[index];
	float3 trTex = mul(inCDir, m);
	trTex = (trTex / 2.0 + 0.5);
	trTex.y = 1 - trTex.y;
	return trTex.xy;
}

#ifdef ALPHA_MASK_ARRAY

float4 sampleColor(int index, float3 cDir, int subIndex = 0)
{
	uint NT = 181;

	float3 tex = float3(transformUV(index % NT, cDir), (index * 2 + subIndex));
	return AlphaMaskArrayTexture.Sample(AlphamaskArraySampler, tex);
}

float4 sampleTree(int index, float3 cDir)
{
	return sampleColor(index, cDir, 1);
}

#define TREE_SCALE 20

float4 FetchImpostorExtras(PixelInterface pixel)
{
	float3 p0 = pixel.custom.cDir / TREE_SCALE; //scaled tree

	float4 t1 = sampleTree(pixel.custom.view_indices.x, p0);
	//float4 t2 = sampleTree(pixel.custom.view_indices.y, p0);
	//float4 t3 = sampleTree(pixel.custom.view_indices.z, p0);
	float4 tIt = t1;//(t1 * pixel.custom.view_blends.x + t2 * pixel.custom.view_blends.y + t3 * pixel.custom.view_blends.z);

	return float4(tIt.z, 0, tIt.w, tIt.r);
}

#ifndef DEPTH_ONLY
float4 FetchImpostorCM(PixelInterface pixel, float4 extras)
{
	float3 p0 = pixel.custom.cDir / TREE_SCALE; //scaled tree

	float4 cm1 = sampleColor(pixel.custom.view_indices.x, p0);
	return float4(cm1.xyz, 0);
	/*float4 cm2 = sampleColor(pixel.custom.view_indices.y, p0);
	float4 cm3 = sampleColor(pixel.custom.view_indices.z, p0);
	float3 cmIt = (cm1 * pixel.custom.view_blends.x + cm2 * pixel.custom.view_blends.y + cm3 * pixel.custom.view_blends.z);

	float3 camDirW = normalize(pixel.custom.cPos);
	float3 newP = p0 -camDirW * extras.w * 0.5f;

	float4 lm1 = sampleTree(pixel.custom.view_indices_light.x, newP);
	float4 lm2 = sampleTree(pixel.custom.view_indices_light.y, newP);
	float4 lm3 = sampleTree(pixel.custom.view_indices_light.z, newP);

	float4 lm = lm1 * pixel.custom.view_blends_light.x + lm2 * pixel.custom.view_blends_light.y + lm3 * pixel.custom.view_blends_light.z;
	lm.r /= (lm.a == 0.0 ? 1.0 : lm.a);

	float d = (lm.r * (2.0 * sqrt(2.0)) - sqrt(2.0)) - dot(normalize(newP), pixel.custom.lDir);
	float kc = 1 - exp(-10.0*max(d, 0.0));

	return float4(cmIt.xyz + cmIt.xyz * kc.xxx * frame_.directionalLightColor, 0);*/
}
#endif

#endif

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#if defined(DITHERED_LOD)
	// discards pixels
	Dither(pixel.screen_position, pixel.custom_alpha);
#endif

    float4 cm;
	float4 extras;
	float4 ng;

#ifdef ALPHA_MASK_ARRAY
	// Impostors
	extras = FetchImpostorExtras(pixel);

	output.depth = OffsetDepth(pixel.screen_position.z, frame_.projection_matrix, 40 * 0);// extras.w);

	float alpha = extras.z;
	clip(alpha - 0.2);
#ifndef DEPTH_ONLY
	if (alpha >= 0.2)
	{
		cm = FetchImpostorCM(pixel, extras);
		ng = float4(0, 0, 1, 1.125);

#ifdef BUILD_TANGENT_IN_PIXEL
        FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, pixel.custom.normal, output, ng, cm, extras);
#else
		FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
#endif
	}
#endif

#else
	output.coverage = AlphamaskCoverageAndClip(0.5f, pixel.custom.texcoord0);

	cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);

#ifdef DEBUG
    cm.xyz *= frame_.TextureDebugMultipliers.RgbMultiplier;
    cm.w *= frame_.TextureDebugMultipliers.MetalnessMultiplier;
#endif

	output.depth = OffsetDepth(output.depth, frame_.projection_matrix, 20 * cm.w * cm.w * cm.w);

#ifndef DEPTH_ONLY
	extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);

#ifdef DEBUG
    ng.w *= frame_.TextureDebugMultipliers.GlossMultiplier;
    extras.x *= frame_.TextureDebugMultipliers.AoMultiplier;
    extras.y *= frame_.TextureDebugMultipliers.EmissiveMultiplier;
    extras.w *= frame_.TextureDebugMultipliers.ColorMaskMultiplier;
#endif

#ifdef BUILD_TANGENT_IN_PIXEL
    FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, pixel.custom.normal, output, ng, cm, extras);
#else
	FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
#endif
#endif

#endif
}
