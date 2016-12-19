#include <Frame.hlsli>
#include <Math/Math.hlsli>

Texture2D<float>	DepthBuffer	: register( t0 );
RWTexture2D<float>  ReprojectedBuffer : register(u0);

float3 reconstructPos(float depth, float2 cspos)
{
    float4 depthCoord = float4(cspos, depth, 1);
    depthCoord = mul (depthCoord, frame_.Environment.inv_proj_matrix);
    
    return depthCoord.xyz / depthCoord.w;
}

float3 constructScreenPos(float3 newPos)
{
    float4 depthCoord = mul (float4(newPos, 1), frame_.Environment.projection_matrix);

    return depthCoord.xyz / depthCoord.w;
}

[numthreads(32, 32, 1)]
void __compute_shader(uint3 id : SV_DispatchThreadID)
{
    int2 oldBufferPos = int2(id.x, id.y);
    if (oldBufferPos.x < frame_.Screen.resolution.x && oldBufferPos.y < frame_.Screen.resolution.y)
    {
        float2 oldScreenPos = float2(oldBufferPos) / frame_.Screen.resolution * 2 - 1;
        float depth = DepthBuffer[oldBufferPos].r;
        float3 oldPos = reconstructPos(depth, oldScreenPos);

        float3x3 rot = rotate_y(-0.175/2);
        float3 newPos = mul(oldPos, rot);
        newPos = oldPos + float3(0.0,0.0,0.5);

        float3 newScreenPos = constructScreenPos(newPos);
        int2 newBufferPos = (newScreenPos.xy + 1) / 2 * frame_.Screen.resolution + 0.5f;
        ReprojectedBuffer[newBufferPos] = newScreenPos.z;
        //InterlockedMax(ReprojectedBuffer[newBufferPos], newScreenPos.z);
        //ReprojectedBuffer[newBufferPos] = newScreenPos.z;
    }
}
