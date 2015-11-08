/*
    FX implementation of Ken Perlin's "Improved Noise"
    sgg 6/26/04
    http://mrl.nyu.edu/~perlin/noise/
*/

// permutation table
static int permutation[] = { 151,160,137,91,90,15,
131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
};

// gradients for 3d noise
static float3 gradient[] = {
    1,1,0,
    -1,1,0,
    1,-1,0,
    -1,-1,0,
    1,0,1,
    -1,0,1,
    1,0,-1,
    -1,0,-1, 
    0,1,1,
    0,-1,1,
    0,1,-1,
    0,-1,-1,
    1,1,0,
    0,-1,1,
    -1,1,0,
    0,-1,-1,
};

// gradients for 4D noise
static float4 g4[] = {
	0, -1, -1, -1,
	0, -1, -1, 1,
	0, -1, 1, -1,
	0, -1, 1, 1,
	0, 1, -1, -1,
	0, 1, -1, 1,
	0, 1, 1, -1,
	0, 1, 1, 1,
	-1, -1, 0, -1,
	-1, 1, 0, -1,
	1, -1, 0, -1,
	1, 1, 0, -1,
	-1, -1, 0, 1,
	-1, 1, 0, 1,
	1, -1, 0, 1,
	1, 1, 0, 1,
	
	-1, 0, -1, -1,
	1, 0, -1, -1,
	-1, 0, -1, 1,
	1, 0, -1, 1,
	-1, 0, 1, -1,
	1, 0, 1, -1,
	-1, 0, 1, 1,
	1, 0, 1, 1,
	0, -1, -1, 0,
	0, -1, -1, 0,
	0, -1, 1, 0,
	0, -1, 1, 0,
	0, 1, -1, 0,
	0, 1, -1, 0,
	0, 1, 1, 0,
	0, 1, 1, 0,
};	


// Textures

texture permTexture
<
    string texturetype = "2D";
    string format = "l8";
	string function = "GeneratePermTexture";
	int width = 256, height = 1;
>;

texture permTexture2d
<
    string texturetype = "2D";
    string format = "a8r8g8b8";
	string function = "GeneratePermTexture2d";
	int width = 256, height = 256;
>;

texture gradTexture
<
    string texturetype = "2D";
	string format = "q8w8v8u8";
	string function = "GenerateGradTexture";
	int width = 16, height = 1;
>;

texture permGradTexture
<
    string texturetype = "2D";
	string format = "q8w8v8u8";
	string function = "GeneratePermGradTexture";
	int width = 256, height = 1;
>;

texture permGrad4dTexture
<
    string texturetype = "2D";
	string format = "q8w8v8u8";
	string function = "GeneratePermGrad4dTexture";
	int width = 256, height = 1;
>;

texture gradTexture4d
<
    string texturetype = "2D";
	string format = "q8w8v8u8";
	string function = "GenerateGradTexture4d";
	int width = 32, height = 1;
>;

// Functions to generate textures using CPU runtime

float4 GeneratePermTexture(float p : POSITION) : COLOR
{
	return permutation[p*256] / 255.0;
}

// 2d permutation texture for optimized version
int perm(int i)
{
	return permutation[i % 256];
}

float4 GeneratePermTexture2d(float2 p : POSITION) : COLOR
{
	p *= 256;
	int A = perm(p.x) + p.y;
	int AA = perm(A);
	int AB = perm(A + 1);
  	int B =  perm(p.x + 1) + p.y;
  	int BA = perm(B);
  	int BB = perm(B + 1);
	return float4(AA, AB, BA, BB) / 255.0;
}

float3 GenerateGradTexture(float p : POSITION) : COLOR
{
	return gradient[p * 16];
}

// permuted gradient texture for optimized version
float3 GeneratePermGradTexture(float p : POSITION) : COLOR
{
	return gradient[permutation[p * 256] % 16];
}

float3 GeneratePermGrad4dTexture(float p : POSITION) : COLOR
{
	return g4[ permutation[p*256] % 32 ];
}

float4 GenerateGradTexture4d(float p : POSITION) : COLOR
{
	return g4[p*32];
}

sampler permSampler = sampler_state 
{
    texture = <permTexture>;
    AddressU  = Wrap;        
    AddressV  = Clamp;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;   
};

sampler permSampler2d = sampler_state 
{
    texture = <permTexture2d>;
    AddressU  = Wrap;        
    AddressV  = Wrap;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;   
};

sampler gradSampler = sampler_state 
{
    texture = <gradTexture>;
    AddressU  = Wrap;        
    AddressV  = Clamp;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;
};

sampler permGradSampler = sampler_state 
{
    texture = <permGradTexture>;
    AddressU  = Wrap;        
    AddressV  = Clamp;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;
};

sampler permGrad4dSampler = sampler_state 
{
    texture = <permGrad4dTexture>;
    AddressU  = Wrap;        
    AddressV  = Clamp;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;
};

sampler gradSampler4d = sampler_state 
{
    texture = <gradTexture4d>;
    AddressU  = Wrap;        
    AddressV  = Clamp;
    MAGFILTER = POINT;
    MINFILTER = POINT;
    MIPFILTER = NONE;
};

float3 fade(float3 t)
{
	return t * t * t * (t * (t * 6 - 15) + 10); // new curve
//	return t * t * (3 - 2 * t); // old curve
}

float4 fade(float4 t)
{
	return t * t * t * (t * (t * 6 - 15) + 10); // new curve
//	return t * t * (3 - 2 * t); // old curve
}

float perm(float x)
{
	return tex1D(permSampler, x);
}

float4 perm2d(float2 p)
{
	return tex2D(permSampler2d, p);
}

float grad(float x, float3 p)
{
	return dot(tex1D(gradSampler, x*16), p);
}

float gradperm(float x, float3 p)
{
	return dot(tex1D(permGradSampler, x), p);
}

// 4d versions
float grad(float x, float4 p)
{
	return dot(tex1D(gradSampler4d, x), p);
}

float gradperm(float x, float4 p)
{
	return dot(tex1D(permGrad4dSampler, x), p);
}

// 3D noise
#if 0

// original version
float inoise(float3 p)
{
	float3 P = fmod(floor(p), 256.0);	// FIND UNIT CUBE THAT CONTAINS POINT
  	p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
	float3 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z.

	P = P / 256.0;
	const float one = 1.0 / 256.0;
	
    // HASH COORDINATES OF THE 8 CUBE CORNERS
  	float A = perm(P.x) + P.y;
  	float4 AA;
  	AA.x = perm(A) + P.z;
	AA.y = perm(A + one) + P.z;
  	float B =  perm(P.x + one) + P.y;
  	AA.z = perm(B) + P.z;
  	AA.w = perm(B + one) + P.z;
 
	// AND ADD BLENDED RESULTS FROM 8 CORNERS OF CUBE
  	return lerp( lerp( lerp( grad(perm(AA.x    ), p ),  
                             grad(perm(AA.z    ), p + float3(-1, 0, 0) ), f.x),
                       lerp( grad(perm(AA.y    ), p + float3(0, -1, 0) ),
                             grad(perm(AA.w    ), p + float3(-1, -1, 0) ), f.x), f.y),
                             
                 lerp( lerp( grad(perm(AA.x+one), p + float3(0, 0, -1) ),
                             grad(perm(AA.z+one), p + float3(-1, 0, -1) ), f.x),
                       lerp( grad(perm(AA.y+one), p + float3(0, -1, -1) ),
                             grad(perm(AA.w+one), p + float3(-1, -1, -1) ), f.x), f.y), f.z);
}

#else

// optimized version
float inoise(float3 p)
{
	float3 P = fmod(floor(p), 256.0);	// FIND UNIT CUBE THAT CONTAINS POINT
  	p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
	float3 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z.

	P = P / 256.0;
	const float one = 1.0 / 256.0;
	
    // HASH COORDINATES OF THE 8 CUBE CORNERS
	float4 AA = perm2d(P.xy) + P.z;
 
	// AND ADD BLENDED RESULTS FROM 8 CORNERS OF CUBE
  	return lerp( lerp( lerp( gradperm(AA.x, p ),  
                             gradperm(AA.z, p + float3(-1, 0, 0) ), f.x),
                       lerp( gradperm(AA.y, p + float3(0, -1, 0) ),
                             gradperm(AA.w, p + float3(-1, -1, 0) ), f.x), f.y),
                             
                 lerp( lerp( gradperm(AA.x+one, p + float3(0, 0, -1) ),
                             gradperm(AA.z+one, p + float3(-1, 0, -1) ), f.x),
                       lerp( gradperm(AA.y+one, p + float3(0, -1, -1) ),
                             gradperm(AA.w+one, p + float3(-1, -1, -1) ), f.x), f.y), f.z);
}

#endif

// 4D noise
float inoise(float4 p)
{
	float4 P = fmod(floor(p), 256.0);	// FIND UNIT HYPERCUBE THAT CONTAINS POINT
  	p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
	float4 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z, W
	P = P / 256.0;
	const float one = 1.0 / 256.0;
	
    // HASH COORDINATES OF THE 16 CORNERS OF THE HYPERCUBE
  	float A = perm(P.x) + P.y;
  	float AA = perm(A) + P.z;
  	float AB = perm(A + one) + P.z;
  	float B =  perm(P.x + one) + P.y;
  	float BA = perm(B) + P.z;
  	float BB = perm(B + one) + P.z;

	float AAA = perm(AA)+P.w, AAB = perm(AA+one)+P.w;
    float ABA = perm(AB)+P.w, ABB = perm(AB+one)+P.w;
    float BAA = perm(BA)+P.w, BAB = perm(BA+one)+P.w;
    float BBA = perm(BB)+P.w, BBB = perm(BB+one)+P.w;

	// INTERPOLATE DOWN
  	return lerp(
  				lerp( lerp( lerp( grad(perm(AAA), p ),  
                                  grad(perm(BAA), p + float4(-1, 0, 0, 0) ), f.x),
                            lerp( grad(perm(ABA), p + float4(0, -1, 0, 0) ),
                                  grad(perm(BBA), p + float4(-1, -1, 0, 0) ), f.x), f.y),
                                  
                      lerp( lerp( grad(perm(AAB), p + float4(0, 0, -1, 0) ),
                                  grad(perm(BAB), p + float4(-1, 0, -1, 0) ), f.x),
                            lerp( grad(perm(ABB), p + float4(0, -1, -1, 0) ),
                                  grad(perm(BBB), p + float4(-1, -1, -1, 0) ), f.x), f.y), f.z),
                            
  				 lerp( lerp( lerp( grad(perm(AAA+one), p + float4(0, 0, 0, -1)),
                                   grad(perm(BAA+one), p + float4(-1, 0, 0, -1) ), f.x),
                             lerp( grad(perm(ABA+one), p + float4(0, -1, 0, -1) ),
                                   grad(perm(BBA+one), p + float4(-1, -1, 0, -1) ), f.x), f.y),
                                   
                       lerp( lerp( grad(perm(AAB+one), p + float4(0, 0, -1, -1) ),
                                   grad(perm(BAB+one), p + float4(-1, 0, -1, -1) ), f.x),
                             lerp( grad(perm(ABB+one), p + float4(0, -1, -1, -1) ),
                                   grad(perm(BBB+one), p + float4(-1, -1, -1, -1) ), f.x), f.y), f.z), f.w);
}


// utility functions

// calculate gradient of noise (expensive!)
float3 inoiseGradient(float3 p, float d)
{
	float f0 = inoise(p);
	float fx = inoise(p + float3(d, 0, 0));	
	float fy = inoise(p + float3(0, d, 0));
	float fz = inoise(p + float3(0, 0, d));
	return float3(fx - f0, fy - f0, fz - f0) / d;
}

// fractal sum
float fBm(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
	float freq = 1.0, amp = 0.5;
	float sum = 0;	
	for(int i=0; i<octaves; i++) {
		sum += inoise(p*freq)*amp;
		freq *= lacunarity;
		amp *= gain;
	}
	return sum;
}

float turbulence(float3 p, int octaves, float4 offsets, float lacunarity = 2.0, float gain = 0.5)
{
	float sum = 0;
	float freq = 1.0, amp = 1.0;
	for(int i=0; i<octaves; i++) 
	{
		sum += abs(inoise(p*freq + offsets[i%4]))*amp;
		freq *= lacunarity;
		amp *= gain;
	}
	return sum;
}


float noiseFog(float3 worldPos, float3 cameraPos, float scale, float density, float4 animation)
{
	float Turbulence = turbulence(worldPos * scale, 4, animation);
	float Density = density * Turbulence;

	// calculating the fog factor
	float f = exp(-pow(Density * distance(cameraPos, worldPos) / FAR_PLANE_DISTANCE, 2.0f));
	return f;
}

float noiseFogNebula(float3 worldPos, float3 cameraPos, float scale, float density, float4 animation)
{
	float Turbulence = turbulence(worldPos * scale, 4, animation);
	float Density = density * Turbulence;

	// calculating the fog factor
	float f = exp(-pow(Density * distance(cameraPos, worldPos), 2.0f));
	return f;
}


// Ridged multifractal
// See "Texturing & Modeling, A Procedural Approach", Chapter 12
float ridge(float h, float offset)
{
    h = abs(h);
    h = offset - h;
    h = h * h;
    return h;
}

float ridgedmf(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5, float offset = 1.0)
{
	float sum = 0;
	float freq = 1.0, amp = 0.5;
	float prev = 1.0;
	for(int i=0; i<octaves; i++) {
		float n = ridge(inoise(p*freq), offset);
		sum += n*amp*prev;
		prev = n;
		freq *= lacunarity;
		amp *= gain;
	}
	return sum;
}
