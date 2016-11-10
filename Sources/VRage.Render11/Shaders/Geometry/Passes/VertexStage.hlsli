#include "PassesDefines.hlsli"

#ifndef RENDERING_PASS
#include "GBuffer/VertexStage.hlsli"
#else

#if RENDERING_PASS == RENDERING_PASS_GBUFFER
#include "GBuffer/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_DEPTH
#include "Depth/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_FORWARD
#include "Forward/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_HIGHLIGHT
#include "Highlight/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_FOLIAGE_STREAMING
#include "FoliageStreaming/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_STATIC_GLASS
#include "StaticGlass/VertexStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_TEST
#include "Test/VertexStage.hlsli"
#endif

#endif
