#include "Declarations.hlsli"
#include <Geometry/PixelTemplateBase.hlsli>
#include <Geometry/Materials/PixelUtilsMaterials.hlsli>

void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
	#if defined(DITHERED) || defined(DITHERED_LOD)
		// Don't do dithering for holograms
		if (pixel.custom_alpha >= 0) 
		{
			// discards pixels
			Dither(pixel.screen_position, pixel.custom_alpha);
		}
	#endif

	#ifndef DEPTH_ONLY
		// for hologram sampling in branch
		if (pixel.custom_alpha < 0)
		{
			// discards pixels
			pixel.color_mul *= Hologram(pixel.screen_position, pixel.custom_alpha);
			output.emissive = 1;
		}

		#ifdef STATIC_DECAL_CUTOUT
			#ifdef USE_TEXTURE_INDICES
				float alphamask = AlphamaskArrayTexture.Sample(TextureSampler, float3(pixel.custom.texcoord0, pixel.custom.texIndices.w));
			#else
				float alphamask = AlphamaskTexture.Sample(TextureSampler, pixel.custom.texcoord0);
			#endif
			clip(alphamask - 0.5);
		#endif

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
        //ng.w = linearizeColor(ng.w);
		#ifdef BUILD_TANGENT_IN_PIXEL
			FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, pixel.custom.normal, output, ng, cm, extras);
		#else
			FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
		#endif
	#endif
}

#include <Geometry/Passes/PixelStage.hlsli>
