#include <frame.hlsli>

struct VertexInput
{
	float4 Position : TEXCOORD0;
};
Texture2D<float>      g_DepthTexture                    : register(t0);

struct VertexInOut 
{
	float4 position	: SV_Position;
};

void __vertex_shader(VertexInput input, out VertexInOut output, uint VertexId : SV_VertexID)
{
    const float2 offsets[ 4 ] =
    {
        float2(-1, 1),
        float2(1, 1),
        float2(-1, -1),
        float2(1, -1),
    };
    uint cornerIndex = VertexId % 4;
    float2 offset = offsets[cornerIndex];
    // collective billboarding
    float4 position = input.Position;
    //float4 position = float4(1,1,-1,1);
    float3 cameraFacingPos = mul(float4(position.xyz, 1), frame_.Environment.view_matrix).xyz;
    cameraFacingPos.xy += position.w * offset / 2;
    float3 wPos = mul(float4(cameraFacingPos, 1), frame_.Environment.inv_view_matrix).xyz;
    output.position = mul(float4(cameraFacingPos, 1), frame_.Environment.projection_matrix);
}

void __pixel_shader(VertexInOut input, out float4 output : SV_Target0)
{
	output = 1;
}
