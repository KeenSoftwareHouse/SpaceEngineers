// @define ROCK_FOLIAGE

#ifdef ROCK_FOLIAGE
#include <Foliage/RockFoliage.h>
#else
#include <Foliage/GrassFoliage.h>
#endif

#include <random.h>

struct VertexStageIn
{
    float3 position : POSITION;
    float4 packed1 : TEXCOORD0; // First two elements for normal, third for Id, fourth for seed
};

struct VertexStageOut
{
    float4 position          : POSITION;
    float3 normal            : NORMAL;
    float4 InstancePosition : TEXCOORD0;
    uint IdSeed             : TEXCOORD1;    // First 8 bits for ID, last 24 for seed
};

struct MaterialFoliage
{
    float2 Scale; 
    float ScaleVariation;   
    uint TextureCount;
};

cbuffer FoliageConstantBuffer : register( MERGE(b, FOLIAGE_SLOT) ) 
{
    MaterialFoliage FoliageConstants[256];
};

Texture2DArray FoliageArray : register ( t0 );
Texture2DArray FoliageNormalArray : register (t1);

void __vertex_shader(VertexStageIn input, out VertexStageOut output)
{
    output.position.xyz = mul(float4(input.position.xyz, 1), get_object_matrix()).xyz;
    output.position.w = 1;
    output.InstancePosition = float4(input.position, 1);
    output.normal = normalize(UnpackNormal(input.packed1.xy));

    output.IdSeed = dot((uint2)(input.packed1.zw * 0xFF), uint2(256, 1));
}

[maxvertexcount(MAX_GEOMETRY_VERTICES)]
void __geometry_shader(point VertexStageOut input[1], inout TriangleStream<RenderingPixelInput> triangle_stream)
{
    float4 position = input[0].position;
    float3 viewVector = get_camera_position() - position.xyz;
    float viewVectorLength = length(viewVector);
    const float far_clip = frame_.foliage_clipping_scaling.x;

    [branch]
    if ( viewVectorLength < far_clip && position.w )
    {
        uint foliageId = input[0].IdSeed & 0xFF;
        uint seed = input[0].IdSeed >> 8;
        float2 hammersleySample = hammersley(seed, 256);

        uint textureIndex = min(floor(hammersleySample.x * FoliageConstants[foliageId].TextureCount), FoliageConstants[foliageId].TextureCount);

        float2 scale = FoliageConstants[foliageId].Scale;
        scale += FoliageConstants[foliageId].ScaleVariation * mad(2, hammersleySample.x, -1) * scale;

        float3 surfaceNormal = input[0].normal;

        float3x3 onb = create_onb(surfaceNormal);
        float3 tanx = onb[0];
        float3 tany = onb[1];

        float2 sinCosHammersley;
        sincos(hammersleySample.y * M_PI * 2.0f, sinCosHammersley.x, sinCosHammersley.y);
        onb = mul(rotate_z(sinCosHammersley.x, sinCosHammersley.y), onb);

        const float angle_min = 0.5f * M_PI_4;
        const float angle_max = M_PI_4 * 0.75f; 


#ifndef ROCK_FOLIAGE
        scale *= lerp(1, frame_.foliage_clipping_scaling.w, saturate((viewVectorLength - frame_.foliage_clipping_scaling.y) / frame_.foliage_clipping_scaling.z));
#endif
        float critical_point = far_clip * 0.5f;
        const float bump = 1.5f;
        scale *= min(mad(bump - 1.0f, smoothstep(0, critical_point, viewVectorLength), 1.0f), bump * smoothstep(0, far_clip - critical_point, far_clip - viewVectorLength));

        float3 N = normalize(mad(3.0f, surfaceNormal, onb[1]));

#ifdef ROCK_FOLIAGE
        SpawnPebble(position.xyz, input[0].InstancePosition.xyz, onb, scale, textureIndex, hammersleySample, viewVectorLength, triangle_stream);
#else

        float3 viewVectorDirection = viewVector / viewVectorLength;
        float f = dot(viewVectorDirection, N);
		float angle = lerp(angle_max, angle_min, saturate(f + 0.2f));
		onb = mul(rotate_x(angle), onb);
        SpawnBillboard(position.xyz, input[0].InstancePosition.xyz, onb, scale, surfaceNormal, textureIndex, triangle_stream);
#endif
    }
}

#include <gbuffer_write.h>
void __pixel_shader(RenderingPixelInput input, out GbufferOutput output)
{
    float4 colorSample = FoliageArray.Sample(TextureSampler, float3(input.texcoord));
#ifdef ROCK_FOLIAGE
#else
    clip(colorSample.w - 0.5f);
#endif
	int material_id = 1;
	float ao = 0.0f + 1.0f * (1 - input.texcoord.y);

	float3 normal = input.normal;
	float gloss = 0.1f;
	float4 ng = FoliageNormalArray.Sample(TextureSampler, float3(input.texcoord));
	gloss = ng.w;
    float3 normalmap = mad(2.0f, ng.xyz, -1.0f) * float3(1, -1, 1);
	
	float3 tangent = input.tangent;
	float3 binormal = cross(tangent, normal);
	
	float3x3 tangent_to_world = float3x3(tangent, binormal, normal);
	normal = normalize(mul(normalmap, tangent_to_world));

    gbuffer_write(output, colorSample.xyz, 0, gloss, normal, material_id, ao); // foliage material is forced in code to be in 1 index-slot
}