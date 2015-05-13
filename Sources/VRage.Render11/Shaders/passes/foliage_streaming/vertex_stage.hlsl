struct VertexStageOutput
{
    float3 position : POSITION;
    float3 normal : NORMAL;
    float3 weights  : TEXCOORD0;
};

void __vertex_shader(__VertexInput input, out VertexStageOutput output)
{
    VertexShaderInterface vertex = __prepare_interface(input);

    MaterialVertexPayload custom;
    vertex_program(vertex, custom);

    output.position = vertex.position_local.xyz;
    output.normal = vertex.normal_world;
    output.weights = vertex.material_weights;
}
