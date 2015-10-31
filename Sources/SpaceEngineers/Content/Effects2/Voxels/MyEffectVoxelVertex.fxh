
float3 VoxelVertex_CellOffset;
float3 VoxelVertex_CellScale;
float3 VoxelVertex_CellRelativeCamera;
float2 VoxelVertex_Bounds;
float  VoxelVertex_MorphDebug;

float VoxelVertex_ComputeMorphParameter(float3 cellRelativePosition)
{
#define MORPH_DEBUG 0
#if MORPH_DEBUG
    return VoxelVertex_MorphDebug;
#else
    float boundsMin = VoxelVertex_Bounds.x;
    float boundsMax = VoxelVertex_Bounds.y;
    float3 diff = abs(cellRelativePosition - VoxelVertex_CellRelativeCamera);
    float dist = max(diff.x, max(diff.y, diff.z));
    return saturate(((dist - boundsMin) / (boundsMax - boundsMin) - 0.4f) / 0.2f);
#endif
}

float3 VoxelVertex_NormalizedToCellRelativePosition( float3 position)
{
    return (position / 32767) * VoxelVertex_CellScale;
}

float3 VoxelVertex_CellRelativeToWorldPosition(float3 position)
{
    //float3 voxelMapWP = WorldMatrix._m30_m31_m32 - VoxelVertex_CellOffset;
    float3 voxelMapWP = WorldMatrix._m30_m31_m32 - VoxelVertex_CellOffset;
    float3 untransformedWP = position + WorldMatrix._m30_m31_m32;

    float3 relative = untransformedWP - voxelMapWP;
    float4x4 rotation = WorldMatrix;
    rotation._m30_m31_m32 = float3(0,0,0);
    relative = mul(relative, rotation);

    return relative + voxelMapWP;
}

float3 VoxelVertex_CellRelativeToLocalPosition(float3 position)
{
    return position + VoxelVertex_CellOffset;
}
