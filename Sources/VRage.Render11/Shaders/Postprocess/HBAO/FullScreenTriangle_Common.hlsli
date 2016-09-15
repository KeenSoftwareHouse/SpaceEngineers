/* 
* Copyright (c) 2008-2016, NVIDIA CORPORATION. All rights reserved. 
* 
* NVIDIA CORPORATION and its licensors retain all intellectual property 
* and proprietary rights in and to this software, related documentation 
* and any modifications thereto. Any use, reproduction, disclosure or 
* distribution of this software and related documentation without an express 
* license agreement from NVIDIA CORPORATION is strictly prohibited. 
*/

#include "ConstantBuffers.hlsli"
#include <Postprocess/Postprocess.hlsli>

//----------------------------------------------------------------------------------
void AddViewportOrigin(inout PostprocessVertex IN)
{
    IN.position.xy += g_f2InputViewportTopLeft;
    IN.uv = IN.position.xy * g_f2InvFullResolution;
}

//----------------------------------------------------------------------------------
void SubtractViewportOrigin(inout PostprocessVertex IN)
{
    IN.position.xy -= g_f2InputViewportTopLeft;
    IN.uv = IN.position.xy * g_f2InvFullResolution;
}
