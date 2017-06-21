#include "..\Debug\Debug.hlsli"
#include "..\Debug\HeatMap.hlsli"

Texture2D<float4> AccumTex : register(t0);

static int numThresholds = 4;
static float4 colorThresholds[] = { float4(0, 0, 0, 1), float4(0, 0, 1, 1), float4(0, 1, 0, 1), float4(1, 0, 0, 1) };
static float intensityThresholds[] = { 0, 10, 50, 500 };

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_TARGET0)
{
	int2 screencoord = vertex.position.xy;
	float intensity = AccumTex.Load(int3(screencoord, 0)).r;

#ifdef USE_GRAYSCALE
	output = intensity / 15;
#else
	//output = 1;
	output = IntensityToHeatMap_4(intensity, colorThresholds, intensityThresholds);
#endif
}