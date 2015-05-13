#ifndef MATH_H__
#define MATH_H__


#define M_PI		3.14159265358979323846
#define M_PI_2     	1.57079632679489661923
#define M_PI_4     	0.785398163397448309616
#define M_1_PI     	0.318309886183790671538
#define M_2_PI     	0.636619772367581343076
#define M_2_SQRTPI 	1.12837916709551257390

// depth for RH projection matrix

float linearize_depth(float depth, float proj33, float proj43) {
	return -proj43 / (depth + proj33);
}

float linearize_depth(float depth, matrix projmatrix) {
	return linearize_depth(depth, projmatrix._33, projmatrix._43);
}

// color space conversions

float3 rgb_to_yc0cg(float3 rgb) {
	static const float3x3 convert_matrix = {
 		1/4., 1/2., 1/4.,
 		1/2., 0, -1/2.,
 		-1/4., 1/2., -1/4.
	};

	return mul(convert_matrix, rgb);
}

float3 yc0cg_to_rgb(float3 yc0cg) {
	return yc0cg.xxx + float3(yc0cg.y -yc0cg.z, yc0cg.z, -yc0cg.y -yc0cg.z);
}

float3 hsv_to_rgb(float3 hsv)
{
    float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0f - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

float3 rgb_to_hsv(float3 rgb)
{
	float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 srgb_to_rgb(float3 srgb)
{
	float3 rgb = (srgb <= 0.04045) * srgb / 12.92;
	rgb += (srgb > 0.04045) * pow((abs(srgb) + 0.055) / 1.055, 2.4);
	return rgb;
}

float3 rgb_to_srgb(float3 rgb)
{
    return pow(rgb, 1/2.2f);
}

float3 rgb_to_XYZ(float3 color)
{
	static const float3x3 convert_matrix = { 0.5141364, 0.3238786, 0.16036376, 0.265068, 0.67023428, 0.06409157, 0.0241188, 0.1228178, 0.84442666 };
	return mul(convert_matrix, color);
}

float3 rgb_to_Yxy(float3 rgb)
{
	float3 XYZ = rgb_to_XYZ(rgb);
	float3 Yxy;
	Yxy.r = XYZ.g;
	float tmp = dot(1, XYZ);
	Yxy.gb = XYZ.rg / tmp;
	return Yxy;
}

float3 Yxy_to_rgb(float3 Yxy)
{
	static const float3x3 convert_matrix = { 2.5651,-1.1665,-0.3986, -1.0217, 1.9777, 0.0439, 0.0753, -0.2543, 1.1892 };
	float3 XYZ = Yxy.r * Yxy.g / Yxy.b;
	XYZ.g = Yxy.r;
	XYZ.b = Yxy.r * (1 - Yxy.g - Yxy.b) / Yxy.b;
	return mul(convert_matrix, XYZ);
}

//

float calc_luminance(float3 rgb)
{
    return dot(rgb, float3(0.299f, 0.587f, 0.114f));
}

// splines

float3 cubic_hermit(float3 p0, float3 p1, float3 m0, float3 m1, float t) {
    float t3 = pow(t, 3);
    float t2 = t*t;
    return (2*t3 - 3*t2 + 1)*p0 + (t3 - 2*t2 + t)*m0 + (t3 - t2)*m1 + (-2*t3 +3*t2)*p1;
}

float3 cubic_hermit_tan(float3 p0, float3 p1, float3 m0, float3 m1, float t) {
    float t2 = t*t;
    return 6*t*(t+1)*p0 + (3*t2 + 4*t + 1)*m0 + t*(3*t-2)*m1 - 6*(t-1)*t*p1;
}

float smootherstep(float edge0, float edge1, float x) {
    // Scale, and clamp x to 0..1 range
    x = clamp((x - edge0)/(edge1 - edge0), 0.0, 1.0);
    // Evaluate polynomial
    return x*x*x*(x*(x*6 - 15) + 10);
}

// curves

float4 smooth_curve( float4 x ) {  
    return x * x *( 3.0 - 2.0 * x );  
}  

float4 triangle_wave( float4 x ) {  
  return abs( frac( x + 0.5 ) * 2.0 - 1.0 );  
}  

float4 smooth_triangle_wave( float4 x ) {  
  return smooth_curve( triangle_wave( x ) );  
}  

// trigonometry

float cos_fast(float x)
{
    x += 1.57079632;
    if (x >  3.14159265)
        x -= 6.28318531;

    float cos;
    if (x < 0)
        cos = 1.27323954 * x + 0.405284735 * x * x;
    else
        cos = 1.27323954 * x - 0.405284735 * x * x;
    return cos;
}

float sin_fast(float x)
{
    if (x < -3.14159265)
        x += 6.28318531;
    else
    if (x >  3.14159265)
        x -= 6.28318531;

    float sin;
    if (x < 0)
        sin = 1.27323954 * x + .405284735 * x * x;
    else
        sin = 1.27323954 * x - 0.405284735 * x * x;
    return sin;
}

// distributions

float gaussian_weigth(float x, float sigma)
{
    const float g = 1.0f / sqrt(2.0f * M_PI * sigma * sigma);
    return (g * exp(-(x * x) / (2 * sigma * sigma)));
}

// matrices
// matrix constructors are column major!

float3x3 create_onb(float3 N)
{
	float3 up = abs(N.y < 0.999) ? float3(0,1,0) : float3(1,0,0);
    float3 tanx = normalize(cross(up, N));
    float3 tany = cross(N, tanx);
	return float3x3( tanx, tany, N );
}

float3x3 rotate_x(float s, float c)
{
	return float3x3(
		1, 0, 0, 
		0, c, s,
		0, -s, c);
}

float3x3 rotate_x(float angle)
{
	float s, c;
	sincos(angle, s, c)	;
	return rotate_x(s, c);
}

float3x3 rotate_y(float s, float c)
{
	return float3x3(
		c, 0, -s, 
		0, 1, 0,
		s, 0, c );
}

float3x3 rotate_y(float angle)
{
	float s, c;
	sincos(angle, s, c)	;
	return rotate_y(s, c);
}

float3x3 rotate_z(float s, float c)
{
	return float3x3(
		c, s, 0, 
		-s, c, 0,
		0, 0, 1 );
}

float3x3 rotate_z(float angle)
{
	float s, c;
	sincos(angle, s, c)	;
	return rotate_z(s, c);
}

// triplanar math

float3 triplanar_weights(float3 n)
{
	float3 w = (abs(n.xyz) - 0.2) * 7;
	return w * rcp(dot(w,w));
}

//

float __solid_angle(float x, float y)
{
	return atan2(x * y, sqrt(x * x + y * y + 1));
}

float texel_coord_solid_angle(float u, float v, int size)
{
	//scale up to [-1, 1] range (inclusive), offset by 0.5 to point to texel center.
	float U = (2.0f * ((float)u + 0.5f) / (float)size) - 1.0f;
	float V = (2.0f * ((float)v + 0.5f) / (float)size) - 1.0f;

	float invsize = 1.0f / size;

	// U and V are the -1..1 texture coordinate on the current face.
	// Get projected area for this texel
	float x0 = U - invsize;
	float y0 = V - invsize;
	float x1 = U + invsize;
	float y1 = V + invsize;
	return __solid_angle(x0, y0) - __solid_angle(x0, y1) - __solid_angle(x1, y0) + __solid_angle(x1, y1);
}

//

static const float3 __cube_axis[] =
{
	float3(1, 0, 0),
	float3(-1, 0, 0),
	float3(0, 1, 0),
	float3(0, -1, 0),
	float3(0, 0, 1),
	float3(0, 0, -1)
};

float3x3 cubemap_face_onb(float faceID)
{
	float3 up = float3(0, 1, 0);
	if (faceID == 2)
	{
		up = float3(0, 0, -1);
	}
	if (faceID == 3)
	{
		up = float3(0, 0, 1);
	}
	float3 right = cross(up, __cube_axis[faceID]);

	return float3x3(right, up, __cube_axis[faceID]);
}

float cubemap_face_id(float3 v)
{
	float face_id = 0;

	if(abs(v.z) >= abs(v.x) && abs(v.z) >= abs(v.y))
		face_id = v.z < 0 ? 5 : 4;
	else if(abs(v.y) >= abs(v.x))
		face_id = v.y < 0 ? 3 : 2;
	else
		face_id = v.x < 0 ? 1 : 0;
	return face_id;
}

//

float radicalInverse(uint bits)
{
    bits = (bits << 16u) | (bits >> 16u);
    bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
    bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
    bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
    bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
    return float(bits) * 2.3283064365386963e-10f;
}

float2 hammersley2d(uint i, uint N)
{
    return float2(float(i) / float(N), radicalInverse(i));
}

#endif