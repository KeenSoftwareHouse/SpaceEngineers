#include <Particles/Globals.h>
#include <Random.h>

// Maximum number of emitters supported
#define NUM_EMITTERS              8
#define NUM_PARTICLE_EMIT_THREADS 128

// A texture filled with random values for generating some variance in our particles when we spawn them
Texture2D                                  g_RandomBuffer            : register(t0);
StructuredBuffer<EmittersStructuredBuffer> g_Emitters                : register(t1);

// The particle buffers to fill with new particles
RWStructuredBuffer<Particle>               g_ParticleBuffer          : register(u0);

// The dead list interpretted as a consume buffer. So every time we consume an index from this list, it automatically decrements the atomic counter (ie the number of dead particles)
ConsumeStructuredBuffer<uint>              g_DeadListToAllocFrom     : register(u1);

cbuffer EmittersConstantBuffer                                       : register(b1)
{
    uint EmittersCount;
	uint ParticleGroupCount;
    float2 pads;
}

// Emit particles, one per thread, in blocks of 128 at a time
[numthreads(NUM_PARTICLE_EMIT_THREADS, NUM_EMITTERS, 1)]
void __compute_shader(uint3 id : SV_DispatchThreadID, uint3 group : SV_GroupID)
{
    // Check to make sure we don't emit more particles than we specified
    uint particleIndex = (group.x * NUM_PARTICLE_EMIT_THREADS) + id.x;
    uint emitterIndex = (group.y * NUM_EMITTERS) + id.y;
    if (emitterIndex < EmittersCount && particleIndex < g_Emitters[emitterIndex].NumParticlesToEmitThisFrame)
    {
        // The index into the global particle list obtained from the dead list. 
        // Calling consume will decrement the counter in this buffer.
		uint index = g_DeadListToAllocFrom.Consume();
        if (index > 0)
        {
            // Initialize the particle data to zero to avoid any unexpected results
            Particle pa = (Particle)0;

            // Generate some random numbers from reading the random texture
            EmittersStructuredBuffer emitter = g_Emitters[emitterIndex];
			float2 uv = float2(id.x / float(NUM_PARTICLE_EMIT_THREADS * ParticleGroupCount), frame_.randomSeed);
			float3 randomValues0 = g_RandomBuffer.SampleLevel(DefaultSampler, uv, 0).xyz;

			float2 uv2 = float2((id.x + 1) / float(NUM_PARTICLE_EMIT_THREADS * ParticleGroupCount), frame_.randomSeed);
			float3 randomValues1 = g_RandomBuffer.SampleLevel(DefaultSampler, uv2, 0).xyz;

			pa.Position = emitter.Position + (randomValues0.xyz * emitter.PositionVariance) + frame_.cameraPositionDelta;
            
            pa.Variation = (randomValues0.x + randomValues1.x + randomValues1.y) / 3;

            pa.EmitterIndex = emitterIndex;
            pa.CollisionCount = 0;

            pa.Velocity = emitter.Velocity + (randomValues1.xyz * emitter.VelocityVariance);
			pa.Age = emitter.ParticleLifeSpan;

			pa.Normal = normalize(emitter.Velocity);
			pa.Origin = emitter.Position;

            // Write the new particle state into the global particle buffer
            g_ParticleBuffer[index] = pa;
        }
    }
}