/* 
* Copyright (c) 2008-2016, NVIDIA CORPORATION. All rights reserved. 
* 
* NVIDIA CORPORATION and its licensors retain all intellectual property 
* and proprietary rights in and to this software, related documentation 
* and any modifications thereto. Any use, reproduction, disclosure or 
* distribution of this software and related documentation without an express 
* license agreement from NVIDIA CORPORATION is strictly prohibited. 
*/

// Number of sampled directions per pixel
#ifndef NUM_DIRECTIONS
#define NUM_DIRECTIONS 8
#endif

// Number of steps per direction
#ifndef NUM_STEPS
#define NUM_STEPS 4
#endif

// To disable the per-pixel randomization
#ifndef USE_RANDOM_TEXTURE
#define USE_RANDOM_TEXTURE 1
#endif

// Width of the tiled random texture
#ifndef RANDOM_TEXTURE_WIDTH
#define RANDOM_TEXTURE_WIDTH 4
#endif

#ifndef GFSDK_PI
#define GFSDK_PI 3.14159265f
#endif

#ifndef MAX_NUM_MRTS
#define MAX_NUM_MRTS 8
#endif
