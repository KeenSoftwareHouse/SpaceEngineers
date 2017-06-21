#include "Globals.hlsli"
#include "Simulation.hlsli"
#include <Random.hlsli>

// Maximum number of emitters supported
#define NUM_EMITTERS              8
#define NUM_PARTICLE_EMIT_THREADS 128

// The particle buffers to fill with new particles
RWStructuredBuffer<Particle>               g_ParticleBuffer          : register(u0);

// The dead list interpretted as a consume buffer. So every time we consume an index from this list, it automatically decrements the atomic counter (ie the number of dead particles)
ConsumeStructuredBuffer<uint>              g_DeadListToAllocFrom     : register(u1);
RWStructuredBuffer<uint>                   g_SkippedParticleCount    : register(u2);

cbuffer EmittersConstantBuffer                                       : register(b1)
{
    uint EmittersCount;
    uint ParticleGroupCount;
    float2 pads;
}

// Emit particles, one per thread, in blocks of 128 at a time
[numthreads(NUM_PARTICLE_EMIT_THREADS, NUM_EMITTERS, 1)]
void __compute_shader(uint3 id : SV_DispatchThreadID)
{
    // Check to make sure we don't emit more particles than we specified
    uint particleIndex = id.x;
    uint emitterIndex = id.y;
    if (emitterIndex < EmittersCount && particleIndex < g_Emitters[emitterIndex].NumParticlesToEmitThisFrame)
    {
        // The index into the global particle list obtained from the dead list. 
        // Calling consume will decrement the counter in this buffer.
        uint index = g_DeadListToAllocFrom.Consume();
        if (index > 0)
        {
			RandomGenerator random;
			random.SetSeed((particleIndex + emitterIndex * NUM_PARTICLE_EMIT_THREADS) + (int)(frame_.randomSeed * 1000000000));
			
            // Initialize the particle data to zero to avoid any unexpected results
            Particle pa = (Particle)0;
            
            EmittersStructuredBuffer emitter = g_Emitters[emitterIndex];

            float simFactor = (float)particleIndex / (float)emitter.NumParticlesToEmitThisFrame;
            float simTime = frame_.frameTimeDelta * simFactor;
            
            pa.EmitterIndex = emitterIndex;
            pa.CollisionCount = 0;

            pa.Variation = random.GetFloatRange(-1, 1);
            pa.Acceleration = mul(emitter.Acceleration, (float3x3)emitter.RotationMatrix) + emitter.Gravity;

            float3 localEmitterDirection = normalize(emitter.Direction);
            float3 particleDirection = localEmitterDirection;
			float3 upVec = float3(0, 1, 0);

            // random direction in specified conus if the emitter were in direction of upVector
            float varAngle = random.GetFloatRange(0, 1) * (1 - emitter.DirectionInnerCone) + emitter.DirectionInnerCone;
			float coneAngle = emitter.DirectionConeVariance * varAngle;
			float3 coneVec = normalize(float3(sin(coneAngle), cos(coneAngle), 0));
			float3x3 coneMat = rotationMatrix(upVec, random.GetFloatRange(-1, 1) * M_PI);
			coneVec = mul(coneMat, coneVec);

			// axis/angle rotation from up vector to emitter direction
            float3 axis = cross(upVec, particleDirection);
            float angle = acos(dot(upVec, particleDirection));

			// transform 
			if (length(axis) < 0.0001f)
			{
                particleDirection = coneVec * sign(particleDirection.y);
			}
			else
			{
				float3x3 mat = rotationMatrix(normalize(axis), angle);
                particleDirection = mul(mat, coneVec);
			}
            float distance = random.GetFloatRange(emitter.EmitterSizeMin, 1) * emitter.Scale;
            float3 localPos = distance * particleDirection * emitter.EmitterSize;
            float3 emitterPos = mul(float4(localPos, 1), emitter.RotationMatrix).xyz;
            pa.Position = emitterPos + frame_.Environment.cameraPositionDelta - emitter.PositionDelta * simFactor;

            float3 velocity = particleDirection * (emitter.Velocity + random.GetFloatRange(-1, 1) * emitter.VelocityVariance) * emitter.Scale;
            pa.Velocity = mul(velocity, (float3x3)emitter.RotationMatrix);
            pa.Age = max(emitter.ParticleLifeSpan + random.GetFloatRange(-1, 1) * emitter.ParticleLifeSpanVar, 0);
            
            pa.RotationVelocity = emitter.RotationVelocity + emitter.RotationVelocityVariance * random.GetFloatRange(-1, 1);

            pa.Normal = mul(localEmitterDirection, (float3x3)emitter.RotationMatrix);
            pa.Origin = float3(emitter.RotationMatrix._41, emitter.RotationMatrix._42, emitter.RotationMatrix._43);

            pa.Age += simTime;
            pa = Simulate(pa, simTime);

            // Write the new particle state into the global particle buffer
            g_ParticleBuffer[index] = pa;
        }
        else g_SkippedParticleCount.IncrementCounter();
    }
}
