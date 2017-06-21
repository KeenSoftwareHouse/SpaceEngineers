#include <Frame.hlsli>

#define COLLISION_THICKNESS 1000.0

#define EMITTERFLAG_STREAKS 1
#define EMITTERFLAG_COLLIDE 2
#define EMITTERFLAG_SLEEPSTATE 4
#define EMITTERFLAG_DEAD 8
#define EMITTERFLAG_LIGHT 0x10
#define EMITTERFLAG_VOLUMETRICLIGHT 0x20
#define EMITTERFLAG_DONOTSIMULATE 0x80
#define EMITTERFLAG_RANDOM_ROTATION_ENABLED 0x200
#define EMITTERFLAG_LOCALROTATION 0x400
#define EMITTERFLAG_LOCALANDCAMERAROTATION 0x800

#define MAX_PARTICLE_COLLISIONS 10

// Emitter structure
// ===================

// look @VRageRender.MyGPUEmitterData for description
struct EmittersStructuredBuffer
{
    float4  Colors[4];

    float2  ColorKeys;
    float   ColorVar;
    float   Scale;

    float3  EmitterSize;
    float   EmitterSizeMin;

    float3  Direction;
    float   Velocity;
    
    float   VelocityVariance;
	float   DirectionInnerCone;
	float   DirectionConeVariance;
    float   RotationVelocityVariance;
	
	float3  Acceleration;
    float   RotationVelocity;

    float3  Gravity;
    float   Bounciness;

    float   ParticleSize[4];

    float2  ParticleSizeKeys;
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
    float   HueVar;
    float   OITWeightFactor;

    matrix  RotationMatrix;

    float3  PositionDelta;
    float   MotionInheritance;

    float3  ParticleRotationRow0;
    float   ParticleLifeSpanVar;
    float3  ParticleRotationRow1;
    float   _pad0;
    float3  ParticleRotationRow2;
    float   _pad1;

    float   ParticleThickness[4];
    float2  ParticleThicknessKeys;
    float   _pad2;
    float   _pad3;
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

    float3  Acceleration;
    float   RotationVelocity;
};

// Bindings
// ==========

// The opaque scene depth buffer read as a texture
Texture2D<float>      g_DepthTexture                    : register(t0);
Texture2D<float4>     g_GBuffer1Texture                 : register(t2);

// A buffer containing the pre-computed view space positions of the particles
StructuredBuffer<EmittersStructuredBuffer> g_Emitters   : register(t1);

// Global functions
// ====================

// Function to calculate the streak radius in X and Y given the particles radius and velocity
float2 calcEllipsoidRadius(float radius, float2 viewSpaceVelocity)
{
    float minRadius = radius * max(1.0f, length(viewSpaceVelocity));
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
float3 UnpackUV(float2 offset, uint textureIndex1, uint textureIndex2, float lifeSpan, float age, float frameTime)
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
    uint animImageIndex = (lifeSpan - age) / frameTime;
    uint animImageOffset = animImageIndex % frameModulo;
    imgOffset += animImageOffset;

    float2 uvOffset = (offset + 1) / 2;
    uvw.x = float(imgOffset % dimX) / dimX + uvOffset.x / dimX;
    uvw.y = float(imgOffset / dimX) / dimY + uvOffset.y / dimY;
    uvw.z = texIndex;

    return uvw;
}