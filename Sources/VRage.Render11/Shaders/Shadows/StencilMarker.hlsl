struct VertexStageIn
{
	float3 position : POSITION;
};

struct PixelStageIn
{
	float4 position : SV_Position;
};

struct MarkerConstants
{
	matrix shadowSpaceToDepthMapSpace;
};



#define MARKER_SLOT b6



cbuffer Marker : register(MARKER_SLOT)
{
	MarkerConstants marker_;
};




void __vertex_shader(VertexStageIn input, out PixelStageIn output)
{
	output.position = mul(float4(input.position.xyz, 1), marker_.shadowSpaceToDepthMapSpace);
}

void __pixel_shader(PixelStageIn input)
{
}
