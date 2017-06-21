#include "Globals.hlsli"
#include "Simulation.hlsli"

// Particle buffer in two parts
RWStructuredBuffer<Particle>               g_ParticleBuffer          : register(u0);

// The dead list, so any particles that are retired this frame can be added to this list
AppendStructuredBuffer<uint>               g_DeadListToAddTo         : register(u1);

// The alive list which gets built using this shader
RWStructuredBuffer<float>                  g_IndexBuffer             : register(u2);

// The draw args for the DrawInstancedIndirect call needs to be filled in before the rasterization path is called, so do it here
RWBuffer<uint>                             g_DrawArgs                : register(u3);

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
        pa.Position -= frame_.Environment.cameraPositionDelta;
        pa.Origin -= frame_.Environment.cameraPositionDelta;

        pa = Simulate(pa, frame_.frameTimeDelta);

        EmittersStructuredBuffer emitter = g_Emitters[pa.EmitterIndex];
        bool killParticle = (emitter.Flags & EMITTERFLAG_DEAD) > 0 || 
            ((emitter.Flags & EMITTERFLAG_SLEEPSTATE) == 0 && pa.CollisionCount >= MAX_PARTICLE_COLLISIONS);

        // Dead particles are added to the dead list for recycling
        if (killParticle || pa.Age < 0.0f)
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

        // Write the particle data back to the global particle buffer
        g_ParticleBuffer[ id.x ] = pa;
    }
}
