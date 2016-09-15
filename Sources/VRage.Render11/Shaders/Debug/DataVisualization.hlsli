Texture2D<uint> Histogram	: register( t0 );

cbuffer Constants : register( c1 )
{
	uint2 Texture_size;
	int Texture_texels;
};

static const float HISTOGRAM_MIN = -8.0f;
static const float HISTOGRAM_MAX = 8.0f;
static const float HISTOGRAM_SPAN = HISTOGRAM_MAX - HISTOGRAM_MIN;
static const uint HISTOGRAM_BINS = 512;
static const uint HISTOGRAM_BAR_MAX = 131072;

#include <Common.hlsli>
#include <Frame.hlsli>
#include <Postprocess/PostprocessBase.hlsli>

