// @defineMandatory NUMTHREADS 16
#include "LightDefs.hlsli"

#ifndef NUMTHREADS_X
#define NUMTHREADS_X NUMTHREADS
#endif

#ifndef NUMTHREADS_Y
#define NUMTHREADS_Y NUMTHREADS_X
#endif

#define GROUP_THREADS NUMTHREADS_X * NUMTHREADS_Y

#include <Frame.hlsli>
#include <GBuffer/GBuffer.hlsli>

cbuffer LightConstants : register( b1 ) {
	uint pointlights_num;
};

StructuredBuffer<PointLightData> LightList : register ( t13 );
RWTexture2D<float4> Output : register( u0 );

RWStructuredBuffer<uint> TileIndices : register(u0);

groupshared uint sMinZ;
groupshared uint sMaxZ;

groupshared uint sTileLightIndices[MAX_TILE_LIGHTS];
groupshared uint sTileNumLights;


[numthreads(NUMTHREADS_X, NUMTHREADS_Y, 1)]
void __compute_shader(
	uint3 dispatchThreadID : SV_DispatchThreadID,
	uint3 groupThreadID : SV_GroupThreadID,
	uint3 GroupID : SV_GroupID,
	uint ThreadIndex : SV_GroupIndex) 
{
    [branch]
	if(ThreadIndex == 0) 
    {
		sMinZ = asuint(1.0f);
		sMaxZ = 0;
		sTileNumLights = 0;
	}

	float minz = 1;
    float maxz = 0;

#ifndef MS_SAMPLE_COUNT
    SurfaceInterface gbuffer = read_gbuffer(frame_.Screen.offset_in_gbuffer + dispatchThreadID.xy);

    minz = min(minz, gbuffer.native_depth);
    maxz = max(maxz, gbuffer.native_depth);
#else
    [unroll] for (uint sample = 0; sample < MS_SAMPLE_COUNT; ++sample) {
		SurfaceInterface gbuffer = read_gbuffer(frame_.Screen.offset_in_gbuffer + dispatchThreadID.xy, sample);

        minz = min(minz, gbuffer.native_depth);
        maxz = max(maxz, gbuffer.native_depth);
    }
#endif

    GroupMemoryBarrierWithGroupSync();
    
    #ifndef COMPLEMENTARY_DEPTH
    if (minz < 1) {
        InterlockedMin(sMinZ, asuint(minz));
        InterlockedMax(sMaxZ, asuint(maxz));
    }
    #else
    if (maxz > 0) {
        InterlockedMin(sMinZ, asuint(minz));
        InterlockedMax(sMaxZ, asuint(maxz));
    }
    #endif

    GroupMemoryBarrierWithGroupSync();

    #ifndef COMPLEMENTARY_DEPTH
        float tileNear = linearize_depth(asfloat(sMinZ), frame_.Environment.projection_matrix);
        float tileFar = linearize_depth(asfloat(sMaxZ), frame_.Environment.projection_matrix);
    #else
        float tileNear = linearize_depth(asfloat(sMaxZ), frame_.Environment.projection_matrix);
        float tileFar = linearize_depth(asfloat(sMinZ), frame_.Environment.projection_matrix);
    #endif


    float2 tile_scale = frame_.Screen.resolution / (float)(2 * NUMTHREADS);
	float2 tile_bias = tile_scale - float2(GroupID.xy);

	float4 c1 = float4(frame_.Environment.projection_matrix._11 * tile_scale.x, 0, -tile_bias.x, 0);
    float4 c2 = float4(0, -frame_.Environment.projection_matrix._22 * tile_scale.y, -tile_bias.y, 0);
    float4 c4 = float4(0, 0, -1, 0);


    float4 frustum_planes[6];
    frustum_planes[0] = c4 - c1;
    frustum_planes[1] = c4 + c1;
    frustum_planes[2] = c4 - c2;
    frustum_planes[3] = c4 + c2;
    frustum_planes[4] = float4(0.0f, 0.0f, -1.0f,  tileNear);
    frustum_planes[5] = float4(0.0f, 0.0f,  1.0f, -tileFar);

	uint i;
    [unroll] for(i=0; i<6; i++) {
    	frustum_planes[i] /= length(frustum_planes[i].xyz);
    }
    
    uint tileIndex = GroupID.y * frame_.Screen.tiles_x + GroupID.x;

    [loop]
	for (uint index = ThreadIndex; index < pointlights_num; index += GROUP_THREADS) {
        PointLightData light = LightList[index];
        float4 vs_light = float4(light.positionView, 1);
                
        bool in_frustum = true;
	    [unroll] for (i = 0; i < 6; ++i) {
	        float d = dot(frustum_planes[i], vs_light);
	        in_frustum = in_frustum && (d >= -light.range);
	    }

        [branch] if (in_frustum) {
            uint listIndex;
            InterlockedAdd(sTileNumLights, 1, listIndex);
            TileIndices[frame_.Screen.tiles_num + tileIndex * MAX_TILE_LIGHTS + listIndex] = index;
        }
    }

    GroupMemoryBarrierWithGroupSync();

    [branch]
	if(ThreadIndex == 0) {
        TileIndices[tileIndex] = sTileNumLights;
    }


	return;
}
