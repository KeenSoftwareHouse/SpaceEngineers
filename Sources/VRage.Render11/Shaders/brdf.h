#include <math.h>
#ifndef BRDF_H__
#define BRDF_H__



float remap_gloss(float g) 
{
	return pow(1-g, 2);
}

float specular_to_roughness(float sp) {
    return sqrt(2.0f / (sp + 2.0f));
}

float roughness_to_specular(float r) {
    return 2.0f / (r * r) - 2.0f;
}

float specular_to_gloss(float sp)
{
	return 1 - sqrt(2.0f / (sp + 2.0f));	
}

float gloss_to_specular(float g)
{
	return roughness_to_specular(1-g);
}

float toksvig_gloss(float g, float normalmap_len)
{
	float sp = gloss_to_specular(g);
	float ft = normalmap_len / lerp(sp, 1.0f, normalmap_len);
	ft = max(ft, 0.001f);
	return specular_to_gloss(ft * sp);
}

float lambert()
{
	return M_1_PI;
}

float oren_nayar(float nl, float nv, float g) {
	float r2 = pow(1-g, 2);
	float A = 1. - 0.5 * r2 / (r2 + 0.57);
	float B = 0.45 * r2 / (r2 + 0.09);
	float C = sqrt((1.0 - nv*nv) * (1.0 - nl*nl)) / max(nv, nl);
	return saturate((A + B * C) * M_1_PI);
}

float3 f_schlick(float3 f0, float vh) 
{
	return f0 + (1-f0)*exp2((-5.55473 * vh - 6.98316)*vh);
}

float v_smithschlick(float nl, float nv, float a) {
	return 1./ ( (nl*(1-a)+a) * (nv*(1-a)+a) );
}

float g_smithschlick(float nl, float nv, float a) {
	return nl * nv * v_smithschlick(nl, nv, a);
}

float d_ggx(float nh, float a) 
{
	float a2 = a*a;
	float denom = pow(nh*nh * (a2-1) + 1, 2);
	return a2 * M_1_PI / denom;
}

float pdf_ggx(float nh, float a)
{
	float a2 = a * a;
	float denom = 1 + (a2 - 1) * nh * nh;
	denom *= denom;
	return a2 * M_1_PI / denom;
}

float3 importance_sample_ggx(float2 xi, float a, float3 N, out float pdf)
{
	float phi = 2 * M_PI * xi.x;
	float costheta = sqrt((1 - xi.y) / (1 + (a*a - 1) * xi.y));
	float sintheta = sqrt(1 - costheta * costheta);
	pdf = pdf_ggx(costheta, a);

	float3 H;
	H.x = sintheta * cos(phi);
	H.y = sintheta * sin(phi);
	H.z = costheta;
	float3 up = abs(N.z) < 0.9999f ? float3(0, 0, 1) : float3(1, 0, 0);
	float3 tanx = normalize(cross(up, N));
	float3 tany = cross(N, tanx);
	
	return tanx * H.x + tany * H.y + N * H.z;
}

// standard brdf

float3 diffuse_brdf(float3 albedo, float g, float nl, float nv) 
{
	return oren_nayar(nl, nv, g) * albedo;
}

float3 specular_brdf(float3 f0, float g, float nl, float nh, float nv, float vh)
{
	float a = remap_gloss(g);
	return d_ggx(nh, a) * saturate(v_smithschlick(nl, nv, a)) * f_schlick(f0, vh) / 4;
}

//

static const float DIELECTRIC_F0 = 0.04f;

float3 surface_albedo(float3 base_color, float metalness)
{
	return lerp(base_color, 0, metalness);
}

float3 surface_f0(float3 base_color, float metalness)
{
	return lerp(DIELECTRIC_F0, base_color, metalness);
}

float3 material_radiance(float3 albedo, float3 f0, float gloss, float3 N, float3 L, float3 V, float3 H)
{
	float nl = saturate(dot(L, N));
	float nv = saturate(dot(N, V));
	float nh = saturate(dot(N, H));
	float vh = saturate(dot(V, H));	

	return diffuse_brdf(albedo, gloss, nl, nv) + specular_brdf(f0, gloss, nl, nh, nv, vh);
}

float3 material_response(float3 base_color, float metalness, float gloss, float id,
	float3 N, float3 L, float3 V, float3 H)
{
	float nl = saturate(dot(L, N));
	float nv = saturate(dot(N, V));
	float nh = saturate(dot(N, H));
	float vh = saturate(dot(V, H));	

	float3 albedo = surface_albedo(base_color, metalness);
	float3 f0 = surface_f0(base_color, metalness);

	return diffuse_brdf(albedo, gloss, nl, nv) + specular_brdf(f0, gloss, nl, nh, nv, vh);		
}

#endif