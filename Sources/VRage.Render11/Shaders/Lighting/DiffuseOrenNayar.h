#ifndef DIFFUSE_OREN_NAYAR__
#define DIFFUSE_OREN_NAYAR__

#include <Math/math.h>
#include <Lighting/Utils.h>

// g -> 1 gives lambertian diffuse
float3 DiffuseLightOrenNayar(float nl, float nv, float3 albedo, float gloss)
{
    float r2 = remap_gloss(gloss);
    float A = mad(-0.5f, r2 / (r2 + 0.33f), 1.0f);
    float B = 0.45f * r2 / (r2 + 0.09f) * max(0, cos(nv - nl));
    float C = sin(max(nv, nl))*tan(min(nl, nv));
    return (albedo * M_1_PI) * cos(nl) * mad(B, C, A);
}
#define DiffuseLight DiffuseLightOrenNayar

#endif