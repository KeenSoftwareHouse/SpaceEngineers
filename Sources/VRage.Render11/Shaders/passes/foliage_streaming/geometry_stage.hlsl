
#include <Foliage/Foliage.h>
#include <random.h>

struct FoliageSet
{
    float Density;
    uint MaterialIndex;
    uint MaterialId;
};

cbuffer FoliageConstantBuffer : register( MERGE(b, FOLIAGE_SLOT) ) 
{
    FoliageSet FoliageConstants;
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

FoliageStreamGeometryOutputVertex SpawnPoint(FoliageStreamVertex vertices[3], uint seed)
{
    float r1 = random(seed);
    float r2 = random(seed);

    float a = 1-sqrt(r1);
    float b = (1-r2)*sqrt(r1);
    float c = r2*sqrt(r1);

    float3 position = vertices[0].position * a + vertices[1].position * b + vertices[2].position * c;

    uint2 packedNormal = pack_normal(normalize(vertices[0].normal + vertices[1].normal + vertices[2].normal));
    
    FoliageStreamGeometryOutputVertex resultVertex;
    resultVertex.position = position;
    resultVertex.NormalSeedMaterialId = packedNormal.x | (packedNormal.y << 8) | ((seed % 0x100) << 16) | (FoliageConstants.MaterialId << 24);
    return resultVertex;
}

float triangle_area(float3 v0, float3 v1, float3 v2)
{
	return length(cross(v1 - v0, v2 - v0)) * 0.5f;
}


uint combine_hashes(uint h0, uint h1) {
    return (17 * 31 + h0) * 31 + h1;
}

uint position_seed(float3 position, uint id) {
    uint3 q = floor(position * 100) / 100;
    q = q % 53629;
	q += id;
    return combine_hashes(q.x, combine_hashes(q.y, q.z));
}

[maxvertexcount(MAX_FOLIAGE_PER_TRIANGLE)]
void __geometry_shader(triangle FoliageStreamVertex input[3], uint primitiveID : SV_PrimitiveID,
    inout PointStream<FoliageStreamGeometryOutputVertex> point_stream)
{
	float3 normal = normalize(input[0].normal + input[1].normal + input[2].normal);

	uint seed = position_seed(input[0].position_world + input[1].position_world * 7 + input[2].position_world * 53, primitiveID);

	float triangleArea = triangle_area(input[0].position, input[1].position, input[2].position);

    uint weightIndex = FoliageConstants.MaterialIndex;
	float weight0 = input[0].weights[weightIndex];
    float weight1 = input[1].weights[weightIndex];
    float weight2 = input[2].weights[weightIndex];

	float averageWeight = (weight0 + weight1 + weight2) / 3.0f;

    float spawnNum = triangleArea * FoliageConstants.Density * averageWeight;
	float spawnFraction = frac(spawnNum);

	uint spawnCount = spawnNum;
    spawnCount = min(spawnCount, MAX_FOLIAGE_PER_TRIANGLE);
    for ( int pointIndex = 0; pointIndex < spawnCount; ++pointIndex )
	{
        point_stream.Append(SpawnPoint(input, seed));
	}
	if ( random(seed) < spawnFraction )
	{
        point_stream.Append(SpawnPoint(input, seed));
	}

	// needed?
	point_stream.RestartStrip();
}

