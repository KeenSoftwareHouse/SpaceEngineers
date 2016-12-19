struct VertexStageOutput
{
    float4 position : SV_Position;
    float3 positionw : POSITION;
    float3 normal : NORMAL;
    float2 texcoord : Texcoord0;
	float custom_alpha : Texcoord1;
};