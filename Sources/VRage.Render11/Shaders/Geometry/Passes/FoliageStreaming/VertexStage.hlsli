#include <Geometry/Foliage/Foliage.hlsli>

void __vertex_shader(__VertexInput input, out FoliageStreamVertex output)
{
    VertexShaderInterface vertex = __prepare_interface(input);

    MaterialVertexPayload custom;
    vertex_program(vertex, custom);

    output.position = vertex.position_scaled_untranslated.xyz;
    output.position_world = vertex.position_local.xyz;
    output.normal = vertex.normal_world;
    output.weights = vertex.material_weights;
}
