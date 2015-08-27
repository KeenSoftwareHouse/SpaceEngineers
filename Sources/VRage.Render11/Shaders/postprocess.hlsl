#include <common.h>
#include <postprocess_base.h>

void fullscreen(uint vertex_id : SV_VertexID, out PostprocessVertex vertex, inout uint instance_id : SV_InstanceID)
{
	vertex.position = float4(-1 + (vertex_id == 2) * 4, -1 + (vertex_id == 1) * 4, 0, 1);
	vertex.uv = float2(0 + (vertex_id == 2) * 2, 1 - 2 * (vertex_id == 1));
}

Texture2D	Source	: register( t0 );

#ifndef MS_SAMPLE_COUNT
Texture2D<uint2>	Stencil		: register( t1 );
#else
Texture2DMS<uint2, MS_SAMPLE_COUNT>		StencilMs	: register( t1 );
#endif

void copy(PostprocessVertex input, out float4 output : SV_Target0)
{
	//#ifndef SOURCE_SRGB
		output = Source[input.position.xy];
	//#else
	//	output = pow(Source[input.position.xy], 2.2f);
	//#endif
}

void clear_alpha(PostprocessVertex input, out float4 output : SV_Target0) {
	output = float4(0,0,0,1);
}

void copyWithStencilTest(PostprocessVertex input, out float4 output : SV_Target0)
{
	output = Source[input.position.xy];
#ifndef MS_SAMPLE_COUNT
	if(Stencil[input.position.xy].g & 0x40) {
		discard;
	}
#else
	[unroll]
	for(uint i=1; i<MS_SAMPLE_COUNT; i++) {
		if(StencilMs.Load(input.position.xy, i).g & 0x40) {
			discard;
		}
	}
#endif
}