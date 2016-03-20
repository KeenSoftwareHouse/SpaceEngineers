
void pixel_program(PixelInterface pixel, inout MaterialOutputInterface output)
{
#if defined(DITHERED) || defined(DITHERED_LOD)
	// discards pixels
	Dither(pixel.screen_position, pixel.custom_alpha);
#endif

#ifndef DEPTH_ONLY
	// for hologram sampling in branch
	if (pixel.custom_alpha < 0)
	{
		// discards pixels
		pixel.color_mul *= Hologram(pixel.screen_position, pixel.custom_alpha);
		output.emissive = 1;
	}

	float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);
#ifdef BUILD_TANGENT_IN_PIXEL
	FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, output, ng, cm, extras);
#else
	FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
#endif
#endif
}
