


float4 GetNearestDistanceAndScale(float distance, float4 materialSettings)
{
	float curDistance = 0;
	float curScale = materialSettings.x;

	float nextDistance = materialSettings.y;
	float nextScale = materialSettings.z;


	while (nextDistance < distance)
	{
		curDistance = nextDistance;
		curScale = nextScale;

		nextDistance *= materialSettings.w;
		nextScale *= materialSettings.z;
	}

	return float4(curDistance, nextDistance, curScale, nextScale);
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