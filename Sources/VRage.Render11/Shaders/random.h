#ifndef RANDOM_H__
#define RANDOM_H__

//

float random2(inout uint seed)
{
    seed = (seed << 13) ^ seed; 
    seed = (seed * ( seed* seed * 15731 + 789221) + 1376312589) & 0x7fffffff;
    return 1 - seed / 1073741824.0f * 0.5;
}

uint wang_hash(inout uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

float rand_lcg(inout uint seed)
{
    seed = 1664525 * seed + 1013904223;
    return seed / (float) 0xFFFFFFFF;
}
 
float rand_xorshift(inout uint seed)
{
    seed ^= (seed << 13);
    seed ^= (seed >> 17);
    seed ^= (seed << 5);
    return seed / 4294967296.0;
}

float random(inout uint seed)
{
	return wang_hash(seed) / (float)0xFFFFFFFF;
}

// quasi-random sequences

float reverse_bits(uint bits) {
     bits = (bits << 16u) | (bits >> 16u);
     bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
     bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
     bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
     bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
     return float(bits) * 2.3283064365386963e-10; // / 0x100000000
 }

 // http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
 float2 hammersley(uint i, uint N) {
     return float2(float(i)/float(N), reverse_bits(i));
 }


#endif