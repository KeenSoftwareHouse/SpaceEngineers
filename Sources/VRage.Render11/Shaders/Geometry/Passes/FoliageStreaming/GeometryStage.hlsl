
#include <Geometry/Foliage/Foliage.hlsli>
#include <Random.hlsli>

struct FoliageSet
{
    float Density;
    uint MaterialIndex;
    uint MaterialId;
    float __padding;
};

cbuffer FoliageConstantBuffer : register( MERGE(b, FOLIAGE_SLOT) ) 
{
    FoliageSet FoliageConstants;
};

FoliageStreamGeometryOutputVertex SpawnPoint(FoliageStreamVertex vertices[3], float r1, float r2, uint r)
{
    float a = 1-sqrt(r1);
    float b = (1-r2)*sqrt(r1);
    float c = r2*sqrt(r1);

    float3 position = a * vertices[0].position + b * vertices[1].position + c * vertices[2].position;

    uint2 packedNormal = PackNormal(normalize(vertices[0].normal + vertices[1].normal + vertices[2].normal));
    
    FoliageStreamGeometryOutputVertex resultVertex;
    resultVertex.position = position;
    resultVertex.NormalSeedMaterialId = packedNormal.x | (packedNormal.y << 8) | ((r % 0x100) << 16) | ((FoliageConstants.MaterialId % 0x100) << 24);
    return resultVertex;
}

float triangle_area(float3 v0, float3 v1, float3 v2)
{
	return length(cross(v1 - v0, v2 - v0)) * 0.5f;
}

uint combine_hashes(uint h0, uint h1) 
{
    return (17 * 31 + h0) * 31 + h1;
}

uint position_seed(float3 position) 
{
    uint3 q = floor(position * 100) / 100;
    q = q % 53629;
    return combine_hashes(q.x, combine_hashes(q.y, q.z));
}

[maxvertexcount(MAX_FOLIAGE_PER_TRIANGLE)]
void __geometry_shader(triangle FoliageStreamVertex input[3], uint primitiveID : SV_PrimitiveID,
    inout PointStream<FoliageStreamGeometryOutputVertex> point_stream)
{
	RandomGenerator random;
	uint seed = position_seed(input[0].position_world + input[1].position_world * 7 + input[2].position_world * 53);
	random.SetSeed(seed);
	
    float triangleArea = triangle_area(input[0].position, input[1].position, input[2].position);

    uint weightIndex = FoliageConstants.MaterialIndex;
	float weight0 = input[0].weights[weightIndex];
    float weight1 = input[1].weights[weightIndex];
    float weight2 = input[2].weights[weightIndex];
	float averageWeight = (weight0 + weight1 + weight2) / 3.0f;

    float spawnNum = triangleArea * FoliageConstants.Density * averageWeight;
    float spawnFraction = frac(spawnNum);

	int spawnCount = spawnNum;
    spawnCount += random.GetFloat() < spawnFraction;
    spawnCount = min(spawnCount, MAX_FOLIAGE_PER_TRIANGLE);
    for ( int pointIndex = 0; pointIndex < spawnCount; ++pointIndex )
	{
        point_stream.Append(SpawnPoint(input, random.GetFloatRange(0, 1), random.GetFloatRange(0, 1), random.GetInt()));
	}

	// needed?
	point_stream.RestartStrip();
}

