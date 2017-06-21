#include "Declarations.hlsli"
#include <Geometry/VertexTemplateBase.hlsli>

void vertex_program(inout VertexShaderInterface vertex, out MaterialVertexPayload custom_output)
{
#if !defined(DEPTH_ONLY) || defined(ALPHA_MASKED)
	custom_output.texcoord0 = vertex.texcoord0;	
	custom_output.normal = vertex.normal_world;
	custom_output.tangent = vertex.tangent_world;
	custom_output.cDir = vertex.cDir;
	custom_output.cPos = vertex.cPos;
	custom_output.lDir = vertex.lDir;
	custom_output.view_indices = vertex.view_indices;
	custom_output.view_blends = vertex.view_blends;
	custom_output.view_indices_light = vertex.view_indices_light;
	custom_output.view_blends_light = vertex.view_blends_light;
#ifdef USE_TEXTURE_INDICES
	custom_output.texIndices = vertex.texIndices;
#endif
#endif
}

#include <Geometry/Passes/VertexStage.hlsli>
