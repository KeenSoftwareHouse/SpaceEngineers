Texture1D<uint> Histogram	: register( t0 );

cbuffer Constants : register( c1 )
{
	uint2 Texture_size;
	int Texture_texels;
};

static const uint HISTOGRAM_BINS = 512;
static const uint HISTOGRAM_BAR_MAX = 131072;

#include <postprocess_base.h>


void display_histogram(PostprocessVertex input, out float4 output : SV_Target0) {
	uint bin = input.uv.x  * HISTOGRAM_BINS;
	uint sample = Histogram[bin];
	float h = (1 - input.uv.y) * HISTOGRAM_BAR_MAX;

	output = sample > (uint)h;
}

void display_func(PostprocessVertex input, out float4 output : SV_Target0) {
	output = 0;
}