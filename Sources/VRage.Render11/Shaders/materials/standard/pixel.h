
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

    float4 ng = NormalGlossTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 extras = AmbientOcclusionTexture.Sample(TextureSampler, pixel.custom.texcoord0);
	float4 cm = ColorMetalTexture.Sample(TextureSampler, pixel.custom.texcoord0);

#ifdef DEBUG
    ng.w *= frame_.TextureDebugMultipliers.GlossMultiplier;
    extras.x *= frame_.TextureDebugMultipliers.AoMultiplier;
    extras.y *= frame_.TextureDebugMultipliers.EmissiveMultiplier;
    extras.w *= frame_.TextureDebugMultipliers.ColorMaskMultiplier;
    cm.xyz *= frame_.TextureDebugMultipliers.RgbMultiplier;
    cm.w *= frame_.TextureDebugMultipliers.MetalnessMultiplier;
#endif

#ifdef BUILD_TANGENT_IN_PIXEL
    FeedOutputBuildTangent(pixel, pixel.custom.texcoord0, pixel.custom.normal, output, ng, cm, extras);
#else
	FeedOutput(pixel, pixel.custom.tangent, pixel.custom.normal, output, ng, cm, extras);
#endif
#endif
}
