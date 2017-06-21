using System;
using VRageMath;
using VRageRender.Animations;

namespace VRageRender.Messages
{
    [Flags]
    public enum GPUEmitterFlags: uint
    {
        // extrude particle in the direction of its current velocity
        Streaks = 1,
        // collide with zbuffer (see Bounciness)
        Collide = 2,
        // go to sleep after 10 bounces?
        SleepState = 4,
        // internal flag
        Dead = 8,
        // use per-vertex lighting (formula: shadow * volumetricLight + ambientDiffuse)
        Light = 0x10,
        // use volumetric lighting based on the particle's initial emitted position and velocity; if not set, volumetricLight = 1
        VolumetricLight = 0x20,
        // do not simulate particles (freeze)
        FreezeSimulate = 0x80,
        // do not emit particles
        FreezeEmit = 0x100,
        // random rotate particles at birth
        RandomRotationEnabled = 0x200,
        // use ParticleRotation for orienting particles, do not billboard at all
        LocalRotation = 0x400,
        LocalAndCameraRotation = 0x800
    }
    // the structure is directly copied to shader buffers - watch for padding!
    public struct MyGPUEmitterData
    {
        // applying the colors = saturate((Color + random(-ColorVar .. ColorVar)) * ColorIntensity
        // color and alpha keys
        public Vector4 Color0, Color1, Color2, Color3;

        // color key positions in particle lifetime 0..1
        public float ColorKey1, ColorKey2;
        // random variance factor for RGB (adding random value to RGB from -ColorVar to ColorVar)
        public float ColorVar;

        // scale of emitter
        public float Scale;

        // emitter ellipse shape size emitting from its volume; local space
        public Vector3 EmitterSize;
        // emitter's shape inner volume size which is not emitting particles
        // (0 - no inner volume, whole emitter volume emits particles, 1 - only surface of emitter is emitting particles)
        public float EmitterSizeMin;

        // direction of emittance; local space
        public Vector3 Direction;
        // initial speed of the emitted particle
        public float Velocity;
        
        // random velocity variance
        public float VelocityVar;
        // emitting conus angle
        public float DirectionInnerCone;
        // emitting variance around the conus angle
        public float DirectionConeVar;
        // rotation velocity variance
        public float RotationVelocityVar;

        // acceleration; local space
        public Vector3 Acceleration;
        // rotation velocity
        public float RotationVelocity;

        // gravity; world space
        public Vector3 Gravity;
        // bounce factor for colliding particles (0 - no bounce, 1 - same output velocity after bounce as input velocity)
        public float Bounciness;

        // radius keys
        public float ParticleSize0, ParticleSize1, ParticleSize2, ParticleSize3;
        // radius key positions in particle lifetime 0..1
        public float ParticleSizeKeys1, ParticleSizeKeys2;
        // internal value updated for shader by render system
        // # of particles to emit this frame
        public int NumParticlesToEmitThisFrame;
        // lifespan in seconds
        public float ParticleLifeSpan;

        // particle softness multiplier, default 1
        public float SoftParticleDistanceScale;
        // streak multiplier
        public float StreakMultiplier;
        // see GPUEmitterFlags
        public GPUEmitterFlags Flags;
        // internal value updated for shader by render system
        // bits 0..7: atlas id, 8..13: atlas #rows, 14..19: atlas #columns, 20..31: image index (0: upper left image)
        public uint TextureIndex1;

        // internal value updated for shader by render system
        // bits 20..31: image animation modulo
        public uint TextureIndex2;
        // time per frame for particle animation (in seconds)
        public float AnimationFrameTime;
        // hue color variance
        public float HueVar;
        // transparency weighted function factor: to help accommodate for very bright particle systems shining from behind other particle systems
        // default: 1, lesser value will make the particle system "shine through" less
        public float OITWeightFactor;

        // used to rotate Direction, Acceleration and EmitterSize; Gravity should be already in world space
        public Matrix RotationMatrix;

        public Vector3 PositionDelta;
        public float MotionInheritance;

        public Vector3 ParticleRotationRow0;
        public float ParticleLifeSpanVar;
        public Vector3 ParticleRotationRow1;
        public float _pad0;
        public Vector3 ParticleRotationRow2;
        public float _pad1;

        // thickness keys
        public float ParticleThickness0, ParticleThickness1, ParticleThickness2, ParticleThickness3;
        // thickness key positions in particle lifetime 0..1
        public float ParticleThicknessKeys1, ParticleThicknessKeys2;
        public float _pad2;
        public float _pad3;

        public void InitDefaults()
        {
            Color0 = Color1 = Color2 = Color3 = Vector4.One;
            ParticleSize0 = ParticleSize1 = ParticleSize2 = ParticleSize3 = 1;
            ColorKey1 = ColorKey2 = 1.0f;
            Direction = Vector3.Forward;
            Velocity = 1.0f;
            ParticleLifeSpan = 1;
            SoftParticleDistanceScale = 1;
            RotationVelocity = 0;
            StreakMultiplier = 4.0f;
            AnimationFrameTime = 1.0f;
            OITWeightFactor = 1.0f;
            Bounciness = 0.5f;
            DirectionInnerCone = 0;
            DirectionConeVar = 0;
            RotationMatrix = Matrix.Identity;
            PositionDelta = Vector3.Zero;
        }
    }

    public struct MyGPUEmitter
    {
        // unique identifier of the emitter, so render system can pair emitted particles with their emitter over multiple frames
        public uint GID;
        // # of particles to emit per second
        public float ParticlesPerSecond;
        // # of particles to burst once
        public float ParticlesPerFrame;

        // path to atlas texture (from content folder as root)
        // all atlases are bundled into one texture array, so they have to have same dimension, pixel format, # mipmaps etc.
        public string AtlasTexture;
        // number of images in atlas per X and Y axis
        public Vector2I AtlasDimension;
        // index of the first frame (frame.X + frame.Y * AtlasDimension.X)
        public int AtlasFrameOffset;
        // # of frames in animation
        public int AtlasFrameModulo;

        // world space position
        public Vector3D WorldPosition;

        public MyGPUEmitterData Data;

        public int MaxParticles() { return (int)(ParticlesPerSecond * Data.ParticleLifeSpan) + 1;  }
    }

    public class MyRenderMessageUpdateGPUEmitters : MyRenderMessageBase
    {
        public MyGPUEmitter[] Emitters = null;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmitters; } }
    }
}
