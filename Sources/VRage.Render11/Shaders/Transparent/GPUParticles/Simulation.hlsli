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

void Collide(in out Particle pa, float frameTimeDelta, float3 vNewPosition, float bounciness)
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

            if (emitter.Flags & EMITTERFLAG_COLLIDE)
                Collide(pa, frameTimeDelta, vNewPosition, emitter.Bounciness);
            else pa.Position = vNewPosition;
        }
    }

    return pa;
}
