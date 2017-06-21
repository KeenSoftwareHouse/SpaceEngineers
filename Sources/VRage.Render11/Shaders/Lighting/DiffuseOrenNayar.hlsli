#ifndef DIFFUSE_OREN_NAYAR__
#define DIFFUSE_OREN_NAYAR__

#include <Math/Math.hlsli>
#include "Utils.hlsli"
/*
// source: https://github.com/stackgl/glsl-diffuse-oren-nayar/blob/master/index.glsl
float3 OrenNayarSimple(float nl, float nv, float lv, float3 albedo, float gloss)
{
	float r2 = remap_gloss(gloss);

	float s = lv - nl * nv;
	float t = lerp(1.0, max(nl, nv), step(0.0, s));

	// albedo??? oversaturation (> 1.0)
	float3 A = 1.0 + r2 * (albedo / (r2 + 0.13) + 0.5 / (r2 + 0.33));
	//float A = mad(-0.5f, r2 / (r2 + 0.33f), 1.0f);


	float B = 0.45 * r2 / (r2 + 0.09);
	float C = s / t;

	return mad(B, C, A);
}


// http://content.gpwiki.org/index.php/D3DBook:%28Lighting%29_Oren-Nayar
float OrenNayar2(float nl, float nv, float gloss)
{
	float r2 = remap_gloss(gloss);

	float A = mad(-0.5f, r2 / (r2 + 0.57f), 1.0f);
	float B = 0.45f * r2 / (r2 + 0.09f) * max(0, dot(v - n * nv, l - n * nl));

	float anv = acos(nv);
	float anl = acos(nl);
	float C = sin(max(anv, anl)) * tan(min(anv, anl));

	return mad(B, C, A);

}
*/

float OrenNayarOriginal(float nl, float nv, float gloss)
{
	float r2 = remap_gloss(gloss);
	float A = mad(-0.5f, r2 / (r2 + 0.33f), 1.0f);
	float B = 0.45f * r2 / (r2 + 0.09f) * max(0, cos(nv - nl));
	float C = sin(max(nv, nl))*tan(min(nl, nv));
	return mad(B, C, A);
}

// gloss -> 1 gives lambertian diffuse
float3 DiffuseLightOrenNayar(float nl, float nv, float lv, float3 albedo, float gloss)
{
	float3 lambert = albedo / 3.1415926;

	return lambert;// * OrenNayarOriginal(nl, nv, gloss);
}

#define DiffuseLight DiffuseLightOrenNayar

#endif
