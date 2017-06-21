#include "AtmosphereCommon.hlsli"
#include <VertexTransformations.hlsli>

void __vertex_shader(float4 vertexPos : POSITION, out float4 svPos : SV_Position)
{
    svPos = mul(unpack_position_and_scale(vertexPos), WorldViewProj);
}
