
float4 IntensityToHeatMap_4(float intensity, float4 colorThresholds[4], float intensityThresholds[4])
{
	static int numThresholds = 4;
	float4 output;
	if (intensity <= intensityThresholds[0])
		output = colorThresholds[0];
	else if (intensity >= intensityThresholds[numThresholds - 1])
		output = colorThresholds[numThresholds - 1];
	else
	{
		for (int i = numThresholds - 1; i >= 1; i--)
		{
			if (intensity < intensityThresholds[i])
			{
				int smallerIndex = i - 1;
				float lerpCoef = (intensity - intensityThresholds[smallerIndex]) / (intensityThresholds[smallerIndex + 1] - intensityThresholds[smallerIndex]);
				output = lerp(colorThresholds[smallerIndex], colorThresholds[smallerIndex + 1], lerpCoef);
			}
		}
	}
	return output;
}

