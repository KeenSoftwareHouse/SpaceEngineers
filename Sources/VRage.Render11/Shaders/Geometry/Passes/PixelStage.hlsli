#include "PassesDefines.hlsli"

#ifndef RENDERING_PASS
#include "GBuffer/PixelStage.hlsli"
#else

#if RENDERING_PASS == RENDERING_PASS_GBUFFER
#include "GBuffer/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_DEPTH
#include "Depth/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_FORWARD
#include "Forward/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_HIGHLIGHT
#include "Highlight/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_FOLIAGE_STREAMING
#include "FoliageStreaming/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_STATIC_GLASS
#include "StaticGlass/PixelStage.hlsli"
#endif

#if RENDERING_PASS == RENDERING_PASS_TEST
#include "Test/PixelStage.hlsli"
#endif

#endif
