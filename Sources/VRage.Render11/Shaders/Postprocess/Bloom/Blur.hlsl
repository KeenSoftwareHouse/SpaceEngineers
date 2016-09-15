// @defineMandatory NUMTHREADS 8

#include "Defines.hlsli"

// this approach is chosen because of stereo rendering of both eyes
cbuffer BlurHParams :register(b1)
{
	int offsetX;
	int maxX;
	float2 resolution;
}

#ifdef HORIZONTAL
static const float2 pixelAlignOffset = float2(1.0f, 0);

float2 GetCoord(uint2 coord, int x, float2 offset)
{
    float2 fTexel = (float2)coord + float2(x, 0) + offset;
    fTexel = clamp(fTexel, 0, float2(maxX, resolution.y));
    fTexel.x += offsetX;
    fTexel /= resolution;
    return fTexel;
}
#else
static const float2 pixelAlignOffset = float2(0, 1.0);
float2 GetCoord(uint2 coord, int y, float2 offset)
{
    float2 fTexel = (float2)coord + float2(0, y) + offset;
    fTexel = clamp(fTexel, 0, float2(maxX, resolution.y));
    fTexel /= resolution;
    return fTexel;
}
#endif

float4 Sample(float2 texel)
{
    return Source.SampleLevel(BilinearSampler, texel, 0);
}

float4 GetWeightedSample(uint2 coord, int index)
{
    float gaussSum = GaussianWeight[index + NUM_GAUSSIAN_SAMPLES] + GaussianWeight[index + NUM_GAUSSIAN_SAMPLES + 1];
    float uvShift = 1 - GaussianWeight[index + NUM_GAUSSIAN_SAMPLES] / gaussSum;
	float4 sample = Sample(GetCoord(coord, index, pixelAlignOffset * uvShift + float2(0.5, 0.5)));
	return sample * gaussSum;
}

float4 GetWeightedSampleOnce(uint2 coord, int index)
{
    float gaussSum = GaussianWeight[index + NUM_GAUSSIAN_SAMPLES];
	float4 sample = Sample(GetCoord(coord, index, float2(0.5, 0.5)));
	return sample * gaussSum;
}

float4 GaussSamplingFast(uint2 coord)
{
	float4 result = 0;
    int i;
    [unroll]
	for(i = -NUM_GAUSSIAN_SAMPLES; i < 0; i += 2)
        result += GetWeightedSample(coord, i);

    [unroll]
	for(i = 1; i <= NUM_GAUSSIAN_SAMPLES; i += 2)
        result += GetWeightedSample(coord, i);

    return result;
}

float4 GaussSamplingSlow(uint2 coord)
{
	float4 result = 0;
    int i;
    [unroll]
	for(i = -NUM_GAUSSIAN_SAMPLES; i < 0; i++)
        result += GetWeightedSampleOnce(coord, i);

    [unroll]
	for(i = 1; i <= NUM_GAUSSIAN_SAMPLES; i++)
        result += GetWeightedSampleOnce(coord, i);

    return result;
}

[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(uint3 dispatchThreadID : SV_DispatchThreadID) 
{
	float4 result;

    float4 sample = Sample(GetCoord(dispatchThreadID.xy, 0, float2(0.5f, 0.5f)));
    result = sample * GaussianWeight[NUM_GAUSSIAN_SAMPLES];

#if NUM_GAUSSIAN_SAMPLES != 0
    //result += GaussSamplingFast(dispatchThreadID.xy);

    result += GaussSamplingSlow(dispatchThreadID.xy);
#endif

	Destination[dispatchThreadID.xy] = result;
}
