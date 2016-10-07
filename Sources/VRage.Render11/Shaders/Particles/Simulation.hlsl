#include <Particles/Globals.h>


// Particle buffer in two parts
RWStructuredBuffer<Particle>               g_ParticleBuffer          : register(u0);

// The dead list, so any particles that are retired this frame can be added to this list
AppendStructuredBuffer<uint>               g_DeadListToAddTo         : register(u1);

// The alive list which gets built using this shader
RWStructuredBuffer<float>                  g_IndexBuffer             : register(u2);

// The draw args for the DrawInstancedIndirect call needs to be filled in before the rasterization path is called, so do it here
RWBuffer<uint>                             g_DrawArgs                : register(u3);

// The opaque scene's depth buffer read as a texture
Texture2D<float>                           g_DepthTexture            : register(t0);

StructuredBuffer<EmittersStructuredBuffer> g_Emitters                : register(t1);

// Calculate the view space position given a point in screen space and a texel offset
float3 calcViewSpacePositionFromDepth(float2 normalizedScreenPosition, int2 texelOffset)
{
    float2 uv;

    // Add the texel offset to the normalized screen position
    normalizedScreenPosition.x += (float)texelOffset.x / frame_.resolution.x;
    normalizedScreenPosition.y += (float)texelOffset.y / frame_.resolution.y;

    // Scale, bias and convert to texel range
    uv.x = (0.5 + normalizedScreenPosition.x * 0.5) * frame_.resolution.x;
    uv.y = (1 - (0.5 + normalizedScreenPosition.y * 0.5)) * frame_.resolution.y;

    // Fetch the depth value at this point
    float depth = g_DepthTexture[uv.xy].r;

    // Generate a point in screen space with this depth
    float4 viewSpacePosOfDepthBuffer;
    viewSpacePosOfDepthBuffer.xy = normalizedScreenPosition.xy;
    viewSpacePosOfDepthBuffer.z = depth;
    viewSpacePosOfDepthBuffer.w = 1;

    // Transform into view space using the inverse projection matrix
    viewSpacePosOfDepthBuffer = mul(viewSpacePosOfDepthBuffer, frame_.inv_proj_matrix);
    viewSpacePosOfDepthBuffer.xyz /= viewSpacePosOfDepthBuffer.w;

    return viewSpacePosOfDepthBuffer.xyz;
}

// Simulate 256 particles per thread group, one thread per particle
[numthreads(256,1,1)]
void __compute_shader(uint3 id : SV_DispatchThreadID)
{
    // Initialize the draw args using the first thread in the Dispatch call
    if (id.x == 0)
    {
        g_DrawArgs[ 0 ] = 0;    // Number of primitives reset to zero
        g_DrawArgs[ 1 ] = 1;    // Number of instances is always 1
        g_DrawArgs[ 2 ] = 0;
        g_DrawArgs[ 3 ] = 0;
        g_DrawArgs[ 4 ] = 0;
    }

    // Wait after draw args are written so no other threads can write to them before they are initialized
    GroupMemoryBarrierWithGroupSync();

    // Fetch the particle from the global buffer
    Particle pa = g_ParticleBuffer[id.x];
    
    // If the partile is alive
    if (pa.Age > 0.0f)
    {
        // Extract the individual emitter properties from the particle
        EmittersStructuredBuffer emitter = g_Emitters[pa.EmitterIndex];

        bool doNotSimulate = (emitter.Flags & EMITTERFLAG_DONOTSIMULATE) > 0;
        float frameTimeDelta = frame_.frameTimeDelta;
        if (doNotSimulate)
            frameTimeDelta = 0;

        // Age the particle by counting down from Lifespan to zero
        pa.Age -= frameTimeDelta;

        float3 vNewPosition = pa.Position;
        vNewPosition -= frame_.cameraPositionDelta;
        pa.Origin -= frame_.cameraPositionDelta;

        // By default, we are not going to kill the particle
        bool killParticle = (emitter.Flags & EMITTERFLAG_DEAD) > 0;

        if (!doNotSimulate)
        {
            // Apply force due to gravity
            if (pa.CollisionCount < MAX_PARTICLE_COLLISIONS)
            {
                pa.Velocity += pa.Acceleration * frameTimeDelta;

                // Calculate the new position of the particle
                vNewPosition += pa.Velocity * frameTimeDelta;

                if (emitter.Flags & EMITTERFLAG_COLLIDE)
                {
                    // Also obtain screen space position
                    float4 screenSpaceParticlePosition = mul(float4(vNewPosition, 1), frame_.view_projection_matrix);
                    screenSpaceParticlePosition.xyz /= screenSpaceParticlePosition.w;

                    // Only do depth buffer collisions if the particle is onscreen, otherwise assume no collisions
                    if (screenSpaceParticlePosition.x > -1 && screenSpaceParticlePosition.x < 1 && screenSpaceParticlePosition.y > -1 && screenSpaceParticlePosition.y < 1)
                    {
                        float3 viewSpaceParticlePosition = mul(float4(vNewPosition, 1), frame_.view_matrix).xyz;
                        float3 viewSpacePosOfDepthBuffer = calcViewSpacePositionFromDepth(screenSpaceParticlePosition.xy, int2(0, 0));
                        if ((viewSpaceParticlePosition.z > viewSpacePosOfDepthBuffer.z) && (viewSpaceParticlePosition.z < viewSpacePosOfDepthBuffer.z + COLLISION_THICKNESS))
                        {
                            // Generate the surface normal. Ideally, we would use the normals from the G-buffer as this would be more reliable than deriving them

                            // Take three points on the depth buffer
                            float3 p0 = viewSpacePosOfDepthBuffer;
                            float3 p1 = calcViewSpacePositionFromDepth(screenSpaceParticlePosition.xy, int2(1, 0));
                            float3 p2 = calcViewSpacePositionFromDepth(screenSpaceParticlePosition.xy, int2(0, 1));

                            // Generate the view space normal from the two vectors
                            float3 viewSpaceNormal = normalize(cross(p2 - p0, p1 - p0));

                            // Transform into world space using the inverse view matrix
                            float3 surfaceNormal = normalize(mul(-viewSpaceNormal, (float3x3)frame_.inv_view_matrix).xyz);

                            // The velocity is reflected in the collision plane
                            float3 newVelocity = reflect(pa.Velocity, surfaceNormal);

                            // Update the velocity and apply some restitution
                            pa.Velocity = emitter.Bounciness * newVelocity;

                            // Update the new collided position
                            vNewPosition = pa.Position + (pa.Velocity * frameTimeDelta);

                            pa.CollisionCount++;
                        }
                    }
                }
            }
            else if ((emitter.Flags & EMITTERFLAG_SLEEPSTATE) == 0)
                killParticle = true;
        }

        // Write the new position
        pa.Position = vNewPosition;

        // Dead particles are added to the dead list for recycling
        if (pa.Age <= 0.0f || killParticle)
        {
            pa.Age = -1;
            g_DeadListToAddTo.Append(id.x);
        }
        else
        {
            // Alive particles are added to the alive list
            uint index = g_IndexBuffer.IncrementCounter();
            g_IndexBuffer[ index ] = (float)id.x;
            
            uint dstIdx = 0;
            // VS only path uses 6 indices per particle billboard
            InterlockedAdd(g_DrawArgs[ 0 ], 6, dstIdx);
        }
    }

    // Write the particle data back to the global particle buffer
    g_ParticleBuffer[ id.x ] = pa;
}
