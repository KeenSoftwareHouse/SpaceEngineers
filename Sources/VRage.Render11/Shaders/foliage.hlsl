// @define ROCK_FOLIAGE

#ifdef ROCK_FOLIAGE
#include <Foliage/RockFoliage.h>
#else
#include <Foliage/GrassFoliage.h>
#endif

#include <random.h>

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

void __vertex_shader(RenderingVertexInput input, out RenderingVertexOutput output)
{
    output.position.xyz = mul(float4(input.position.xyz, 1), get_object_matrix()).xyz;
    output.position.w = 1;
    output.InstancePosition = float4(input.position, 1);
    output.normal = normalize(UnpackNormal(input.NormalSeedMaterialId.xy));

    output.IdSeed = dot((uint2)(input.NormalSeedMaterialId.zw * 0xFF), uint2(256, 1));
}

[maxvertexcount(MAX_GEOMETRY_VERTICES)]
void __geometry_shader(point RenderingVertexOutput input[1], inout TriangleStream<RenderingPixelInput> triangle_stream)
{
    float4 position = input[0].position;
    float3 viewVector = get_camera_position() - position.xyz;
    float viewVectorLength = length(viewVector);
    const float far_clip = frame_.foliage_clipping_scaling.x;

    [branch]
    if ( viewVectorLength < far_clip && position.w )
    {
        uint foliageId = input[0].IdSeed & 0xFF;
        uint seed = (input[0].IdSeed >> 8);
        float2 hammersleySample = hammersley(seed, 256);

        uint textureIndex = min(floor(hammersleySample.x * FoliageConstants[foliageId].TextureCount), FoliageConstants[foliageId].TextureCount);

        float2 scale = FoliageConstants[foliageId].Scale;
        scale += FoliageConstants[foliageId].ScaleVariation * mad(2, hammersleySample.x, -1) * scale;

        float3 surfaceNormal = input[0].normal;

        float3x3 onb = create_onb(surfaceNormal);
        //float3 surfaceTangent = onb[0];
        float3 surfaceBinormal = onb[1];

        float2 sinCosHammersley;
        sincos(hammersleySample.y * M_PI * 2.0f, sinCosHammersley.x, sinCosHammersley.y);
        onb = mul(rotate_z(sinCosHammersley.x, sinCosHammersley.y), onb);

#ifndef ROCK_FOLIAGE
        scale *= lerp(1, frame_.foliage_clipping_scaling.w, saturate((viewVectorLength - frame_.foliage_clipping_scaling.y) / frame_.foliage_clipping_scaling.z));
#endif

        float critical_point = far_clip * 0.5f;
        const float bump = 1.5f;
        scale *= min(mad(bump - 1.0f, smoothstep(0, critical_point, viewVectorLength), 1.0f), bump * smoothstep(0, far_clip - critical_point, far_clip - viewVectorLength));

#ifdef ROCK_FOLIAGE
        SpawnPebble(position.xyz, input[0].InstancePosition.xyz, onb, scale, textureIndex, hammersleySample, viewVectorLength, triangle_stream);
#else
        // Bend the grass a little towards the tangent plane
        float3 adjustedNormal = normalize(mad(3.0f, surfaceNormal, surfaceBinormal));
        float3 viewVectorDirection = viewVector / viewVectorLength;

        const float angle_min = 0.5f * M_PI_4;
        const float angle_max = 0.75f * M_PI_4;

        const float f = dot(viewVectorDirection, adjustedNormal);
		float angle = lerp(angle_max, angle_min, saturate(f + 0.2f));
		onb = mul(rotate_x(angle), onb);
        SpawnBillboard(position.xyz, input[0].InstancePosition.xyz, scale, surfaceNormal, onb[0], textureIndex, triangle_stream);
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