#include <VertexTransformations.hlsli>

float2 getScreenUV(float2 normalizedScreenPosition)
{
    // Scale, bias and convert to texel range
    return float2(
        (0.5 + normalizedScreenPosition.x * 0.5) * frame_.Screen.resolution.x,
        (1 - (0.5 + normalizedScreenPosition.y * 0.5)) * frame_.Screen.resolution.y);
}

// Calculate the view space position given a point in screen space and a texel offset
float3 calcViewSpacePositionFromDepth(float2 normalizedScreenPosition)
{
    float2 uv = getScreenUV(normalizedScreenPosition);

    // Fetch the depth value at this point
    float depth = g_DepthTexture[uv].r;

    // Generate a point in screen space with this depth
    float4 viewSpacePosOfDepthBuffer;
    viewSpacePosOfDepthBuffer.xy = normalizedScreenPosition.xy;
    viewSpacePosOfDepthBuffer.z = depth;
    viewSpacePosOfDepthBuffer.w = 1;

    // Transform into view space using the inverse projection matrix
    viewSpacePosOfDepthBuffer = mul(viewSpacePosOfDepthBuffer, frame_.Environment.inv_proj_matrix);
    viewSpacePosOfDepthBuffer.xyz /= viewSpacePosOfDepthBuffer.w;

    return viewSpacePosOfDepthBuffer.xyz;
}
float3 fetchNormal(float2 normalizedScreenPosition)
{
    float2 uv = getScreenUV(normalizedScreenPosition);
    float2 gbuffer1 = g_GBuffer1Texture[uv].xy;
    float3 nview = unpack_normals2(gbuffer1.xy);
    return view_to_world(nview);
}

void Collide(in out Particle pa, float frameTimeDelta, float3 vNewPosition, float bounciness)
{
    // Also obtain screen space position
    float4 screenSpaceParticlePosition = mul(float4(vNewPosition, 1), frame_.Environment.view_projection_matrix);
    screenSpaceParticlePosition.xyz /= screenSpaceParticlePosition.w;

    // Only do depth buffer collisions if the particle is onscreen, otherwise assume no collisions
    if (screenSpaceParticlePosition.x > -1 && screenSpaceParticlePosition.x < 1 && screenSpaceParticlePosition.y > -1 && screenSpaceParticlePosition.y < 1)
    {
        float3 viewSpaceParticlePosition = mul(float4(vNewPosition, 1), frame_.Environment.view_matrix).xyz;
        float3 viewSpacePosOfDepthBuffer = calcViewSpacePositionFromDepth(screenSpaceParticlePosition.xy);
        if ((viewSpaceParticlePosition.z > viewSpacePosOfDepthBuffer.z - COLLISION_THICKNESS) && (viewSpaceParticlePosition.z < viewSpacePosOfDepthBuffer.z))
        {
            // Generate the surface normal. Ideally, we would use the normals from the G-buffer as this would be more reliable than deriving them
            float3 surfaceNormal = fetchNormal(screenSpaceParticlePosition.xy);

            // The velocity is reflected in the collision plane
            float3 newVelocity = reflect(pa.Velocity, surfaceNormal);

            // Update the velocity and apply some restitution
            pa.Velocity = bounciness * newVelocity;

            // Update the new collided position
            vNewPosition = pa.Position + pa.Velocity * frameTimeDelta;

            pa.CollisionCount++;
        }
    }
    pa.Position = vNewPosition;
}

Particle Simulate(Particle pa, float frameTimeDelta)
{
    // Extract the individual emitter properties from the particle
    EmittersStructuredBuffer emitter = g_Emitters[pa.EmitterIndex];

    bool doNotSimulate = (emitter.Flags & EMITTERFLAG_DONOTSIMULATE) > 0;

    if (!doNotSimulate)
    {
        // Age the particle by counting down from Lifespan to zero
        pa.Age -= frameTimeDelta;

        // Apply force due to gravity
        if (pa.CollisionCount < MAX_PARTICLE_COLLISIONS)
        {
            float3 vNewPosition = pa.Position;
            
            pa.Velocity += pa.Acceleration * frameTimeDelta;

            // Calculate the new position of the particle
            vNewPosition += pa.Velocity * frameTimeDelta;
            vNewPosition += emitter.PositionDelta * emitter.MotionInheritance;

            if (emitter.Flags & EMITTERFLAG_COLLIDE)
                Collide(pa, frameTimeDelta, vNewPosition, emitter.Bounciness);
            else pa.Position = vNewPosition;
        }
    }

    return pa;
}
