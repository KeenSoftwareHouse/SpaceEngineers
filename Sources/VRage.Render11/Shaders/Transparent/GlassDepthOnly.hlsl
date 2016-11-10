#include <Geometry/Passes/StaticGlass/Declarations.hlsli>

#include <Frame.hlsli>
#include <VertexTransformations.hlsli>

void __pixel_shader(VertexStageOutput vertex, out float2 normal : SV_TARGET0)
{
    // Set normals on gbuffer1 copy
    float3 nview = world_to_view(vertex.normal);
    normal = pack_normals2(normalize(nview));
}
