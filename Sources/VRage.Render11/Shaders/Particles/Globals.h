#include <frame.h>

#define COLLISION_THICKNESS 1

#define EMITTERFLAG_STREAKS 1
#define EMITTERFLAG_COLLIDE 2
#define EMITTERFLAG_SLEEPSTATE 4
#define EMITTERFLAG_DEAD 8
#define EMITTERFLAG_LIGHT 0x10
#define EMITTERFLAG_VOLUMETRICLIGHT 0x20

#define MAX_PARTICLE_COLLISIONS 10

// Emitter structure
// ===================

struct EmittersStructuredBuffer
{
    float4  Colors[4];
    float2  ColorKeys;
    float2  AlphaKeys;

    float3  Position;
	float   Bounciness;

    float3  PositionVariance;
	float   OITWeightFactor;

    float3  Velocity;
    float   VelocityVariance;

    float3  Acceleration;
    float   RotationVelocity;

    float   Size[4];

    float2  SizeKeys;
    uint    NumParticlesToEmitThisFrame;
    float   ParticleLifeSpan;

    float   SoftParticleDistanceScale;
    float   StreakMultiplier;
    uint    Flags;
	// format: 8b atlas texture array index, 6b atlas dimensionX, 6b atlas dimensionY, 12b image index in atlas
    uint    TextureIndex1;
	
	// bits 20..31: image animation modulo
	uint    TextureIndex2;
	// time per frame for animated particle image
	float   AnimationFrameTime;
	float   __Pad1, __Pad2;
};

// Particle structures
// ===================

struct Particle
{
    float3  Position;               // World space position
    float   Variation;              // Random value 0..1

    float3  Velocity;               // World space velocity
	uint    EmitterIndex;           // The index of the emitter in 0-15 bits, 16-23 for atlas index, 24th bit is whether or not the emitter supports velocity-based streaks
	
	// lighting:
	float3  Normal;
	float   Age;                    // The current age counting down from lifespan to zero
	float3  Origin;
	uint    CollisionCount;         // Keep track of how many times the particle has collided
};

// Function to calculate the streak radius in X and Y given the particles radius and velocity
float2 calcEllipsoidRadius(float radius, float multiplier, float2 viewSpaceVelocity)
{
    float minRadius = radius * max(1.0f, multiplier * length(viewSpaceVelocity));
    return float2(radius, minRadius);
}

// this creates the standard Hessian-normal-form plane equation from three points, 
// except it is simplified for the case where the first point is the origin
float3 CreatePlaneEquation(float3 b, float3 c)
{
    return normalize(cross(b, c));
}

// point-plane distance, simplified for the case where 
// the plane passes through the origin
float GetSignedDistanceFromPlane(float3 p, float3 eqn)
{
    // dot(eqn.xyz, p.xyz) + eqn.w, , except we know eqn.w is zero 
    // (see CreatePlaneEquation above)
    return dot(eqn, p);
}

float Interpolate(float factor, float2 keys, out int index)
{
    float step = smoothstep(0, keys.x, factor) + 
        smoothstep(keys.x, keys.y, factor) +
        smoothstep(keys.y, 1.0f, factor);
    index = trunc(step);
    return frac(step);
}

#define ATLAS_INDEX_BITS 12
#define ATLAS_DIMENSION_BITS 6
#define ATLAS_TEXTURE_BITS 8
float3 UnpackUV(float2 offset, uint textureIndex1, uint textureIndex2, float age, float frameTime)
{
	float3 uvw;
	uint imgOffset = textureIndex1 & ((1 << ATLAS_INDEX_BITS) - 1);
	textureIndex1 >>= ATLAS_INDEX_BITS;
	uint dimY = textureIndex1 & ((1 << ATLAS_DIMENSION_BITS) - 1);
	textureIndex1 >>= ATLAS_DIMENSION_BITS;
	uint dimX = textureIndex1 & ((1 << ATLAS_DIMENSION_BITS) - 1);
	textureIndex1 >>= ATLAS_DIMENSION_BITS;
	uint texIndex = textureIndex1 & ((1 << ATLAS_TEXTURE_BITS) - 1);

	uint frameModulo = textureIndex2 & ((1 << ATLAS_INDEX_BITS) - 1);
	uint animImageIndex = age / frameTime;
	uint animImageOffset = animImageIndex % frameModulo;
	imgOffset += animImageOffset;

	float2 uvOffset = (offset + 1) / 2;
	uvw.x = float(imgOffset % dimX) / dimX + uvOffset.x / dimX;
	uvw.y = float(imgOffset / dimX) / dimY + uvOffset.y / dimY;
	uvw.z = texIndex;

	return uvw;
}