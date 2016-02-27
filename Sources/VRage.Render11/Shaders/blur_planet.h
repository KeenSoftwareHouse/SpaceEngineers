#include <common.h>
#include <postprocess_base.h>

#include <gbuffer.h>

#ifndef MS_SAMPLE_COUNT
Texture2D<float3> Input : register(t5);
#else
Texture2DMS<float3, MS_SAMPLE_COUNT> Input : register(t5);
#endif

#include <frame.h>

cbuffer Blur : register(b1)
{
	float MaxDeviation;
	float MaxDepth;
	float TransitionRatio;
	float __padding;
};

#define RANGE 2
#define SAMPLES 6
#define DEVIATION 2
#define MAX_DEPTH 10000

bool is_mask(float2 pos)
{
#ifndef MS_SAMPLE_COUNT
	float sample = Gbuffer2[pos].y;
#else
	float sample = Gbuffer2.Load(pos, 0).y;
#endif

	return sample * 255 == 4;
}

float get_depth(float2 pos)
{
	float hw_depth;
#ifndef MS_SAMPLE_COUNT
	hw_depth = DepthBuffer[pos].r;
#else
	hw_depth = DepthBuffer.Load(pos, 0).r;
#endif

	return -linearize_depth(hw_depth, frame_.projection_matrix);
}

float3 sample_input(float2 pos)
{
#ifndef MS_SAMPLE_COUNT
	return Input[pos];
#else
	return Input.Load(pos, 0);
#endif
}

float3 blur(float2 position, float2 mask)
{
	float depth = get_depth(position);

	float3 result = 0;
	float total_weight = 0;

	float deviation = MaxDeviation;
	if (depth > MaxDepth && depth < MaxDepth * (1.0f + TransitionRatio))
	{
		deviation = deviation * (depth - MaxDepth) / (MaxDepth * TransitionRatio);
	}

	if (depth < MaxDepth)
	{
		deviation = 0;
	}
	
	if (deviation > 0.01)
	{
		float blur_factor = 0;
		for (int i = -SAMPLES; i < SAMPLES; i++)
		{
			float2 pos = position + mask * i * RANGE;
			if (is_mask(pos) && get_depth(pos) > MaxDepth)
			{
				blur_factor++;
			}
		}

		if (blur_factor < 2)
		{
			result = sample_input(position);
		}
		else
		{
			deviation *= blur_factor / (SAMPLES * 2);
			for (int i = -SAMPLES; i < SAMPLES; i++)
			{
				float2 pos = position + mask * i * RANGE;
				float weight = GaussianWeight(i, deviation);

				result += sample_input(pos) * weight;
				total_weight += weight;
			}
			result *= rcp(total_weight);
		}
	}
	else
	{
		result = sample_input(position);
	}
	return result;
}
