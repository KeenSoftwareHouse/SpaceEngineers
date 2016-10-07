#ifndef MATH_H__
#define MATH_H__


#define M_PI		3.14159265358979323846
#define M_PI_2     	1.57079632679489661923
#define M_PI_4     	0.785398163397448309616
#define M_1_PI     	0.318309886183790671538
#define M_2_PI     	0.636619772367581343076
#define M_2_SQRTPI 	1.12837916709551257390

// depth for RH projection matrix

float linearize_depth(float depth, float proj33, float proj43) 
{
	return -proj43 / (depth + proj33);
}

float linearize_depth(float depth, matrix projmatrix) 
{
	return linearize_depth(depth, projmatrix._33, projmatrix._43);
}

float OffsetDepth(float depth, matrix projmatrix, float offset)
{
	float proj43 = projmatrix._43;
	float proj33 = projmatrix._33;
	float linearDepth = linearize_depth(depth, proj33, proj43);
	float newDepth = linearDepth + offset;

	return -proj43 / newDepth - proj33;
}

// splines

float3 cubic_hermit(float3 p0, float3 p1, float3 m0, float3 m1, float t) 
{
    float t3 = pow(t, 3);
    float t2 = t*t;
    return (2*t3 - 3*t2 + 1)*p0 + (t3 - 2*t2 + t)*m0 + (t3 - t2)*m1 + (-2*t3 +3*t2)*p1;
}

float3 cubic_hermit_tan(float3 p0, float3 p1, float3 m0, float3 m1, float t) 
{
    float t2 = t*t;
    return 6*t*(t+1)*p0 + (3*t2 + 4*t + 1)*m0 + t*(3*t-2)*m1 - 6*(t-1)*t*p1;
}

float smootherstep(float edge0, float edge1, float x) 
{
    // Scale, and clamp x to 0..1 range
    x = clamp((x - edge0)/(edge1 - edge0), 0.0, 1.0);
    // Evaluate polynomial
    return x*x*x*(x*(x*6 - 15) + 10);
}

// curves

float4 smooth_curve( float4 x ) 
{
    return x * x *( 3.0 - 2.0 * x );  
}  

float4 triangle_wave( float4 x ) 
{
  return abs( frac( x + 0.5 ) * 2.0 - 1.0 );  
}  

float4 smooth_triangle_wave( float4 x ) 
{
  return smooth_curve( triangle_wave( x ) );  
}  

// distributions

float ExponentialDensity(float x, float rateParameter)
{
    return x > 0 ? rateParameter * exp(-rateParameter * x) : 0;
}

float CalcGaussianWeight(float x, float sigma)
{
    const float sigmaSq = sigma * sigma;
    const float normalizationFactor = 1.0f / sqrt(2.0f * M_PI * sigmaSq);
    return normalizationFactor * exp(-(x * x) / (2 * sigmaSq));
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

float3x3 rotationMatrix(float3 axis, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float3x3(oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.x * axis.z + axis.y * s,
        oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
        oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c);
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


float3 project(float3 projectedOntoVector, float3 projectedVector)
{
	float dotProduct = dot(projectedVector, projectedOntoVector);
	return (dotProduct / length(projectedOntoVector)) * projectedOntoVector;
}

float rand(float2 co)
{
	return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float CalcSoftParticle(float softFactor, float targetDepth, float particleDepth)
{
    float depthFade = 1;
    depthFade = particleDepth - targetDepth;
    clip(depthFade);
    depthFade = saturate(depthFade / (softFactor * 0.3f));
    return depthFade;
}

float4x4 CreateScaleMatrix(float s1, float s2, float s3)
{
    return float4x4(
        s1, 0, 0, 0,
        0, s2, 0, 0,
        0, 0, s3, 0,
        0, 0, 0, 1);
}

float4x4 CreateScaleMatrix(float s)
{
    return float4x4(
        s, 0, 0, 0,
        0, s, 0, 0,
        0, 0, s, 0,
        0, 0, 0, 1);
}

float4x4 CreateTranslationMatrix(float3 t)
{
    return float4x4(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        t, 1);
}

#if NUM_GAUSSIAN_SAMPLES == 0
static const float GaussianWeight[] = { 1 };
#elif NUM_GAUSSIAN_SAMPLES == 1
static const float GaussianWeight[] = { 0.319466,	0.361069,	0.319466 };
#elif NUM_GAUSSIAN_SAMPLES == 2
static const float GaussianWeight[] = { 0.153388, 0.221461,	0.250301,	0.221461,	0.153388};
#elif NUM_GAUSSIAN_SAMPLES == 3
static const float GaussianWeight[] = { 0.071303, 0.131514, 0.189879, 0.214607, 0.189879, 0.131514, 0.071303};
#elif NUM_GAUSSIAN_SAMPLES == 4
static const float GaussianWeight[] = { 0.028532, 0.067234, 0.124009, 0.179044, 0.20236, 0.179044, 0.124009, 0.067234, 0.028532};
#elif NUM_GAUSSIAN_SAMPLES == 5
static const float GaussianWeight[] = { 0.0093, 0.028002, 0.065984, 0.121703, 0.175713, 0.198596, 0.175713, 0.121703, 0.065984, 0.028002, 0.0093};
#elif NUM_GAUSSIAN_SAMPLES == 6
static const float GaussianWeight[] = { 0.002406, 0.009255, 0.027867, 0.065666, 0.121117, 0.174868, 0.197641, 0.174868, 0.121117, 0.065666, 0.027867, 0.009255, 0.002406 };
#elif NUM_GAUSSIAN_SAMPLES == 7 // sigma 3
static const float GaussianWeight[] = { 0.009033, 0.018476, 0.033851, 0.055555, 0.08167, 0.107545, 0.126854, 0.134032, 0.126854, 0.107545, 0.08167, 0.055555, 0.033851, 0.018476, 0.009033};
#elif NUM_GAUSSIAN_SAMPLES == 8
static const float GaussianWeight[] = { 0.003924, 0.008962, 0.018331, 0.033585, 0.055119, 0.081029, 0.106701, 0.125858, 0.13298, 0.125858, 0.106701, 0.081029, 0.055119, 0.033585, 0.018331, 0.008962, 0.003924};
#elif NUM_GAUSSIAN_SAMPLES == 9 // sigma 4
static const float GaussianWeight[] = { 0.008162, 0.013846, 0.022072, 0.033065, 0.046546, 0.061573, 0.076542, 0.089414, 0.098154, 0.101253, 0.098154, 0.089414, 0.076542, 0.061573, 0.046546, 0.033065, 0.022072, 0.013846, 0.008162};
#elif NUM_GAUSSIAN_SAMPLES == 10
static const float GaussianWeight[] = { 0.004481, 0.008089, 0.013722, 0.021874, 0.032768, 0.046128, 0.061021, 0.075856, 0.088613, 0.097274, 0.100346, 0.097274, 0.088613, 0.075856, 0.061021, 0.046128, 0.032768, 0.021874, 0.013722, 0.008089, 0.004481};
#endif

#endif
