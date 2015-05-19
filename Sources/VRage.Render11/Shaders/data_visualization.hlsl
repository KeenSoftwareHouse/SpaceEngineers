Texture1D<uint> Histogram	: register( t0 );
Texture2D<float> AvgLuminance : register( t1 );

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

#include <common.h>
#include <frame.h>
#include <postprocess_base.h>

float logLumToBinPos(float ll) {
	return (ll - HISTOGRAM_MIN) / HISTOGRAM_SPAN * HISTOGRAM_BINS;
}

float middle_grey(float x) {
	if(frame_.middle_grey > 0) 
		return frame_.middle_grey;

	float A = frame_.middle_grey_curve_sharpness; // sharpness
	float C = frame_.middle_grey_0; // at 0
	const float B = C + A * 0.5;
	return B - A / (2 + log10(x + 1));
}


void display_histogram(PostprocessVertex input, out float4 output : SV_Target0) {
	//
	float avg = log(AvgLuminance[uint2(0,0)].r);

	float Y = 1 - input.uv.y;

	uint bin = input.uv.x * HISTOGRAM_BINS;
	uint sample = Histogram[bin];
	float h = Y * Histogram[512] * 1.05f;

	float interval_bins = HISTOGRAM_BINS / HISTOGRAM_SPAN;
	float xBin = 0.5 + bin; // useful for pixel cutting in x coord

	float4 barColor = (xBin > logLumToBinPos(frame_.logLumThreshold)) ? 1 : float4(0.1f, 0, 0, 1); 

	output = (sample > (uint)h) * barColor;
	bool center = (bin == 255 || bin == 256);

	float r = fmod(xBin, interval_bins);
	r = min(r, interval_bins - r);
	r *= r < 1;

	float d0 = abs(xBin - HISTOGRAM_BINS * 0.5);
	d0 *= d0 < 1;

	output += r * float4(1,1,1,0) * 0.05;
	output += d0 * float4(1,0,0,0);

	float ad = abs(xBin - logLumToBinPos(avg));
	ad *= ad < 1;

	output += ad * float4(0,1,0,0);

	float xLogLum = input.uv.x * HISTOGRAM_SPAN + HISTOGRAM_MIN;
	float yMG = middle_grey(exp(xLogLum));
	float4 funcMgVal = (abs(yMG - Y) < 0.01f) * float4(0, 0, 1, 0);
	output = any(funcMgVal) ? funcMgVal : output;
}

float3 curve_tonemap(float3 x)
{
	const float A = frame_.tonemapping_A;
	const float B = frame_.tonemapping_B;
	const float C = frame_.tonemapping_C;
	const float D = frame_.tonemapping_D;
	const float E = frame_.tonemapping_E;
	const float F = frame_.tonemapping_F;

	return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
}

void display_tonemapping(PostprocessVertex input, out float4 output : SV_Target0) {
	float x = float(input.uv.x * 5);
	float y = (1 - input.uv.y);
	float f_x = curve_tonemap(x.xxx).x;
	output = (y < f_x) * float4(1,1,1,0) * 0.25f;
	output += (abs(y - x) < 0.01f) * float4(0,1,0,0);
}