


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
	float3 lightDir = normalize(frame_.directionalLightVec);
	float3 nrm = normalize(pixel.custom.normal.xyz);
	float shadowTreshold = -0.2f;

	/*if (dot(lightDir, nrm) > shadowTreshold)
	{
		output.base_color = float3(1, 0, 0);

	}
	else
	{
		output.base_color = float3(0, 1, 0);
	}

	output.metalness = 0;

	output.normal = nrm;
	output.gloss = 0;
	output.emissive = 0;
	output.ao = 1;
	output.id = 4;

	return;*/

#ifdef DITHERED

	bool isDarkSide = dot(lightDir, nrm) > shadowTreshold;

	float object_dither = abs(pixel.custom_alpha);
	float tex_dither = Dither8x8[(uint2)pixel.screen_position.xy % 8];

	if (object_dither > 2)
	{
		object_dither -= 2.0f;
		object_dither = 2.0f - object_dither;

		if (object_dither > 1)
		{
			object_dither -= 1;

#ifdef DEPTH_ONLY
			if (tex_dither > object_dither && !isDarkSide)
			{
				DISCARD_PIXEL;
			}
#endif
		}
		else
		{ //0 - 1
#ifdef DEPTH_ONLY
			if (!isDarkSide)
			{
				DISCARD_PIXEL;
			}
#else
			if (tex_dither > object_dither)
			{
				DISCARD_PIXEL;
			}
#endif
		}
	}
	else
	{
		if (object_dither > 1)
		{
#ifdef DEPTH_ONLY
			if (!isDarkSide)
			{
				DISCARD_PIXEL;
			}
#else

			object_dither -= 1;
			if (tex_dither < object_dither)
			{
				DISCARD_PIXEL;
			}
#endif
		}
		else // 1 - 2
		{
#ifdef DEPTH_ONLY
			if (!isDarkSide)
			{
				if (tex_dither < object_dither)
				{
					DISCARD_PIXEL;
				}
			}
#endif
		}
	}
#endif
}