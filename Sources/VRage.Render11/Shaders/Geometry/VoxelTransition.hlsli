// triplanar math

float3 triplanar_weights(float3 n)
{
    float3 w = saturate(abs(n) - 0.55);
    return w / dot(w, 1);

    /*float3 w = saturate(abs(n) - 0.2) * 7;
    w *= w;
    w *= w;
    return w / dot(w, 1);*/

    /*float3 w = abs(n.xyz) - 0.2;
	return w / dot(w, 1);*/
}

void ProcessDithering(PixelInterface pixel, inout MaterialOutputInterface output)
{
#ifdef DITHERED
	float3 lightDir = normalize(frame_.directionalLightVec);
	float3 nrm = normalize(pixel.custom.normal.xyz);
	float shadowTreshold = -0.2f;

	// < 0 dark side; >0 light side
	float voxelSide = shadowTreshold - dot(lightDir, nrm);

	float tex_dither = Dither8x8[(uint2)pixel.screen_position.xy % 8];
	float object_dither = abs(pixel.custom_alpha);
	if (object_dither > 2)
	{
		object_dither -= 2.0f;
		object_dither = 2.0f - object_dither;

		if (object_dither > 1)
		{
#ifdef DEPTH_ONLY
			object_dither -= 1;

			if (voxelSide > 0)
				clip(object_dither - tex_dither);

#endif
		}
		else
		{ //0 - 1
#ifdef DEPTH_ONLY
			clip(-voxelSide);
#else
			clip(object_dither - tex_dither);
#endif
		}
	}
	else
	{
		if (object_dither > 1)
		{
#ifdef DEPTH_ONLY
			clip(-voxelSide);
#else

			object_dither -= 1;
			clip(tex_dither - object_dither);
#endif
		}
		else // 1 - 2
		{
#ifdef DEPTH_ONLY
			if (voxelSide > 0)
			{
				clip(tex_dither - object_dither);
			}
#endif
		}
	}
#endif
}