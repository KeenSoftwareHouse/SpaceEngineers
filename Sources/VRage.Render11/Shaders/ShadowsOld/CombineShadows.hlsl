#include <PostProcess/PostprocessBase.hlsli>
#include <GBuffer/GBuffer.hlsli>
#include "Csm.hlsli"

Texture2D<float> FirstTexture : register(t0);
Texture2DArray<float> SecondTextureArray : register(t1);

cbuffer FirstCascadeConstantBuffer : register (b10)
{
    CsmConstants FirstCascadeConstants;
}

cbuffer SecondCascadeConstantBuffer : register (b11)
{
    CsmConstants SecondCascadeConstants;
}

struct InverseProjectionInfo
{
    matrix InverseCascadeMatrix;
};

cbuffer FirstCascadeInverseConstantBuffer : register(b12)
{
    InverseProjectionInfo FirstCascadeInverseConstants;
};

float __pixel_shader(PostprocessVertex input) : SV_Target0
{
    //return Stencil[input.uv * frame_.Screen.resolution].y == 31 ? 0 : 1;
    //return Stencil[input.position.xy].y;
    //return (Stencil[input.position.xy].y;
    float2 firstTexel = input.position.xy;
    float firstSample = FirstTexture[input.position.xy];
    //float firstSample = FirstTexture[input.position.xy];
    float3 firstWorldPosition = ShadowmapToWorld(input.position.xy, FirstCascadeInverseConstants.InverseCascadeMatrix);

    /*int cascadeId = cascade_id_stencil(Stencil[input.position.xy].g);
    if ( cascadeId == 32 )
        return firstSample;
    int secondCascadeIndex = cascadeId;*/

    int secondCascadeIndex = 0;
#ifndef MS_SAMPLE_COUNT
    secondCascadeIndex = cascade_id_stencil(Stencil[input.uv * frame_.Screen.resolution].g);
#else
    secondCascadeIndex = cascade_id_stencil(Stencil.Load(input.position.xy, 0).g);
#endif
    //int secondCascadeIndex = cascade_index_by_split(linearize_depth(input.position.z, frame_.Environment.projection_matrix));
    float3 secondTexel = WorldToShadowmap(firstWorldPosition, SecondCascadeConstants.cascade_matrix[secondCascadeIndex]);
    float secondSample = SecondTextureArray[float3(secondTexel.xy, secondCascadeIndex)];

    return min(firstSample, secondSample);
    //return min(firstSample, SecondTextureArray.Sample(PointSampler, float3(secondTexel.xy, secondCascadeIndex)));
}