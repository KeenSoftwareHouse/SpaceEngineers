#include "Debug.hlsli"
#include "HeatMap.hlsli"

Texture2D<float4>	HdrTex : register(t5);

static int numThresholds = 4;
static float4 colorThresholds[] = { float4(0, 0, 1, 1), float4(0, 1, 0, 1), float4(1, 0, 0, 1), float4(1, 1, 1, 1) };
static float intensityThresholds[] = { 0, 1, 5, 10 };

void __pixel_shader(PostprocessVertex vertex, out float4 output : SV_Target0)
{
	int3 screencoord = int3(vertex.position.xy, 0);
	float4 hdrColor = HdrTex.Load(screencoord);
	float intensity = .2126 * hdrColor.r + .7152 * hdrColor.g + .0722 * hdrColor.b;

	output = IntensityToHeatMap_4(intensity, colorThresholds, intensityThresholds);
}
