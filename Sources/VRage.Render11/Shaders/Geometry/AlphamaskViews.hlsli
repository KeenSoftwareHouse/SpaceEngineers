#ifndef ALPHAMASKVIEWS_CONSTANTS__
#define ALPHAMASKVIEWS_CONSTANTS__
#include <Common.hlsli>

struct AlphamaskConstants {
	//
	matrix 	impostor_view_matrix[181];	
};

cbuffer AlphamaskViews : register( MERGE(b,ALPHAMASK_SLOT) )
{
	AlphamaskConstants alphamask_constants;
};


static const int N = 9;
static const int NT = 2 * N * (N + 1) + 1;


int viewNumber(int i, int j)
{
	return i * ((2 * N + 1) - abs(i)) + j + (N * (N + 1));
}

void findViews(int species, float3 cDIR, inout int3 vv, inout float3 rr)
{
	float3 VDIR = float3(cDIR.x, max(-cDIR.y, 0.001f), cDIR.z);
	float a = abs(VDIR.x) > abs(VDIR.z) ? -VDIR.z / VDIR.x : -VDIR.x / -VDIR.z;
	float ac = N * (float)acos(saturate(VDIR.y)) / 3.141592657f;
	float nxx = (1.0f - a) * ac;// uniform sampling in theta
	float nyy = (1.0f + a) * ac;
	int i = (int)floor(nxx);
	int j = (int)floor(nyy);
	float ti = nxx - i;
	float tj = nyy - j;
	float alpha = 1.0f - ti - tj;
	bool b = alpha > 0.0;
	float3 ii = float3(b ? i : i + 1, i + 1, i);
	float3 jj = float3(b ? j : j + 1, j, j + 1);
	rr = float3(abs(alpha), b ? ti : 1.0 - tj, b ? tj : 1.0 - ti);
	if (abs(VDIR.z) >= abs(VDIR.x))
	{
		float3 tmp = ii;
		ii = -jj;
		jj = tmp;
	}
	if (abs(VDIR.x + -VDIR.z) > 0.00001f)
	{
		ii *= (int)sign(VDIR.x + -VDIR.z);
		jj *= (int)sign(VDIR.x + -VDIR.z);
	}
	vv = (species * NT).xxx + int3(viewNumber(ii.x, jj.x), viewNumber(ii.y, jj.y), viewNumber(ii.z, jj.z));
}


#endif
