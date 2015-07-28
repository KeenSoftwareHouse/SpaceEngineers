struct VertexStageOutput
{
    float3 position : POSITION;
    float3 position_world : POSITION1;
    float3 normal 	: NORMAL;
    float3 weights  : TEXCOORD0;
};

struct SO_Vertex
{
	//uint2 packed0 : TEXCOORD0;
    float3 position : TEXCOORD0;
    uint packed1 : TEXCOORD1;
};

struct FoliageSet
{
    float density;
    uint weight_index;
    uint material_id;
};

#define FOLIAGE_SLOT 4
cbuffer FoliageConstants : register( b4 ) 
{
    FoliageSet foliage_;
};

uint2 packed_float4(float4 p)
{
	return uint2(f32tof16(p.x) | (f32tof16(p.y) << 16), f32tof16(p.z) | (f32tof16(p.w) << 16 ));
}

uint2 pack_normal(float3 v)
{
	uint2 p = (v.xz * 0.5 + 0.5) * uint2(255, 127);
	p.y |= (1 << 7) * (v.y > 0);
	return p;
}

#include <random.h>

SO_Vertex spawn_point(VertexStageOutput V[3], inout uint seed)
{
    float r1 = random(seed);
    float r2 = random(seed);

    float a = 1-sqrt(r1);
    float b = (1-r2)*sqrt(r1);
    float c = r2*sqrt(r1);

    float3 position = V[0].position * a + V[1].position * b + V[2].position * c;

    uint2 packed_n = pack_normal(normalize(V[0].normal + V[1].normal + V[2].normal));
    
    SO_Vertex result;
    result.position = position;
    result.packed1 = packed_n.x | (packed_n.y << 8) | ((seed % 0x100) << 16) | (foliage_.material_id << 24);
    return result;
}

float triangle_area(float3 v0, float3 v1, float3 v2)
{
    return length(cross(v0, v1) + cross(v1, v2) + cross(v2, v0)) * 0.5;
}

#define MAX_ELEMENTS_NUM 24

uint combine_hashes(uint h0, uint h1) {
    return (17 * 31 + h0) * 31 + h1;
}

uint position_seed(float3 position) {
    uint3 q = floor(position * 100) / 100;
    q = q % 53629;
    return combine_hashes(q.x, combine_hashes(q.y, q.z));
}

[maxvertexcount(MAX_ELEMENTS_NUM)]
void gs( triangle VertexStageOutput input[3], uint primitiveID : SV_PrimitiveID, 
    inout PointStream<SO_Vertex> point_stream)
{
    SO_Vertex output;

    float3 normal = normalize(input[0].normal + input[1].normal + input[2].normal);
    uint2 packed_n = pack_normal(normal);

    //uint seed = primitiveID;
    //float3 min_position = min(input[0].position, min(input[1].position, input[2].position));
    uint seed = position_seed(input[0].position_world + input[1].position_world * 7 + input[2].position_world * 53);

    float area = triangle_area(input[0].position, input[1].position, input[2].position);

    float w0 = input[0].weights[foliage_.weight_index];
    float w1 = input[1].weights[foliage_.weight_index];
    float w2 = input[2].weights[foliage_.weight_index];

    float w = (w0 + w1 + w2) / 3;

    float g = area * foliage_.density * w;
    float P = frac(g);

    int num = g;
    num = min(num, MAX_ELEMENTS_NUM);
    for(int i=0; i< num; i++)
    {
        point_stream.Append(spawn_point(input, seed));
    }
    if(random(seed) < P)
    {
        point_stream.Append(spawn_point(input, seed));
    }

    // needed?
    point_stream.RestartStrip();
}

