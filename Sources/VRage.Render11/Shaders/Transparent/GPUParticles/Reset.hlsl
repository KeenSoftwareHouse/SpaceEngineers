#include "Globals.hlsli"

// Particle buffer in two parts
RWStructuredBuffer<Particle>    g_ParticleBuffer        : register( u0 );

// Reset 256 particles per thread group, one thread per particle
[numthreads(256,1,1)]
void __compute_shader(uint3 id : SV_DispatchThreadID)
{
    g_ParticleBuffer[ id.x ] = (Particle)0;
}
