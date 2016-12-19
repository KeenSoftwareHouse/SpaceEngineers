#include "Declarations.hlsli"

void __vertex_shader(__VertexInput input, out VertexStageOutput output)
{
    VertexShaderInterface vertex = __prepare_interface(input);

    MaterialVertexPayload custom;
    vertex_program(vertex, custom);

    output.position = vertex.position_clip;
    output.positionw = vertex.position_local.xyz;
    output.normal = custom.normal;
    output.texcoord = custom.texcoord0;
    output.custom_alpha = vertex.custom_alpha;
}
