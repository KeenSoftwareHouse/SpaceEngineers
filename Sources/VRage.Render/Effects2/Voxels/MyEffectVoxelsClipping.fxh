/*
WorldMatrix is not correct transformation matrix, just container for values used by shaders rendering voxel meshes.
Data layout is as follows:
    offset_x    offset_y    offset_z    bounds_min
    scale_x     scale_y     scale_z     bounds_max
    camera_x    camera_y    camera_z    ----------
    position_x  position_y  position_z  ----------

offset, scale: used to denormalize vertex positions
position: local to world transformation
bounds: used for clipping pixels in pixel shader
camera: camera position in local coordinates

WorldMatrix variable should be defined in the .fx file itself, before these functions are included.
*/

float VoxelVertex_ComputeMorphParameter(float3 localPosition)
{
    // return WorldMatrix._m23;

    float3 localCamera = WorldMatrix._m20_m21_m22;
    float3 diff = abs(localPosition - localCamera);
    float dist = max(diff.x, max(diff.y, diff.z));
    float boundsMin = WorldMatrix._m03;
    float boundsMax = WorldMatrix._m13;
    return saturate(((dist - boundsMin) / (boundsMax - boundsMin) - 0.4f) / 0.3f);
}

void VoxelVertex_NormalizedToLocalPosition(inout float3 position)
{
    position = (position / 32767) * WorldMatrix._m10_m11_m12 + WorldMatrix._m00_m01_m02;
}

void VoxelVertex_LocalToWorldPosition(inout float3 position)
{
    position += WorldMatrix._m30_m31_m32;
}

void VoxelVertex_WorldToLocalPosition(inout float3 position)
{
    position -= WorldMatrix._m30_m31_m32;
}
