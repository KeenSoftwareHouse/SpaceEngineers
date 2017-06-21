#ifndef POSTPROCESS_BASE__
#define POSTPROCESS_BASE__

struct PostprocessVertex 
{
	float4 position	: SV_Position;
	float2 uv		: TEXCOORD0;
};

#endif
