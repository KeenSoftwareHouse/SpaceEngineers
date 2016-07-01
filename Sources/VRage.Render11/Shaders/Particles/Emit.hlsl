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
RWStructuredBuffer<uint>                   g_SkippedParticleCount    : register(u2);

cbuffer EmittersConstantBuffer                                       : register(b1)
{
    uint EmittersCount;
    uint ParticleGroupCount;
    float2 pads;
}

float3x3 rotationMatrix(float3 axis, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float3x3(oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
        oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
        oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c);
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
            // Initialize the particle data to zero to avoid any unexpected results
            Particle pa = (Particle)0;

            // Generate some random numbers from reading the random texture
            EmittersStructuredBuffer emitter = g_Emitters[emitterIndex];
			float2 uv = float2((particleIndex + emitterIndex * NUM_PARTICLE_EMIT_THREADS) / float(NUM_PARTICLE_EMIT_THREADS * NUM_EMITTERS), frame_.randomSeed);
            float3 randomValues0 = g_RandomBuffer.SampleLevel(DefaultSampler, uv, 0).xyz;

			float2 uv2 = float2(((NUM_PARTICLE_EMIT_THREADS - particleIndex - 1) + emitterIndex * NUM_PARTICLE_EMIT_THREADS) / float(NUM_PARTICLE_EMIT_THREADS * NUM_EMITTERS), frame_.randomSeed);
            float3 randomValues1 = g_RandomBuffer.SampleLevel(DefaultSampler, uv2, 0).xyz;

            float rndLength = length(randomValues0);
            float3 rndNormal = randomValues0 / rndLength;
            float3 vec = (saturate(rndLength) * (1 - emitter.EmitterSizeMin) + emitter.EmitterSizeMin) * rndNormal;
            float3 localPos = vec * emitter.EmitterSize * emitter.Scale;
            float3 emitterPos = mul(float4(localPos, 1), emitter.RotationMatrix).xyz;
            pa.Position = emitterPos + frame_.cameraPositionDelta;

            pa.Acceleration = mul(emitter.Acceleration, (float3x3)emitter.RotationMatrix) + emitter.Gravity;

            pa.Variation = randomValues0.x;// +randomValues1.x + randomValues1.y) / 3;

            pa.EmitterIndex = emitterIndex;
            pa.CollisionCount = 0;

            float3 worldDirection = mul(normalize(emitter.Direction), (float3x3)emitter.RotationMatrix);
            float3 particleDirection = worldDirection;
			float3 upVec = float3(0, 1, 0);

            // random direction in specified conus if the emitter were in direction of upVector
			float coneAngle = emitter.DirectionCone + randomValues1.x * emitter.DirectionConeVariance;
			float3 coneVec = normalize(float3(sin(coneAngle), cos(coneAngle), 0));
			float3x3 coneMat = rotationMatrix(upVec, randomValues1.y * M_PI * 1.5f);
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
            pa.Velocity = particleDirection * (emitter.Velocity + randomValues1.z * emitter.VelocityVariance) * emitter.Scale;
            pa.Age = emitter.ParticleLifeSpan;
            
            pa.RotationVelocity = emitter.RotationVelocity + emitter.RotationVelocityVariance * (randomValues0.y + randomValues1.y) / 2;

            pa.Normal = worldDirection;
            pa.Origin = float3(emitter.RotationMatrix._41, emitter.RotationMatrix._42, emitter.RotationMatrix._43);

            // Write the new particle state into the global particle buffer
            g_ParticleBuffer[index] = pa;
        }
        else g_SkippedParticleCount.IncrementCounter();
    }
}
