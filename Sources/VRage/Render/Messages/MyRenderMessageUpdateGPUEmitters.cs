using System;
using VRageMath;

namespace VRageRender
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
        VolumetricLight = 0x20
    }
    // the structure is directly copied to shader buffers - watch for padding!
    public struct MyGPUEmitterData
    {
        // color and alpha keys
        public Vector4 Color0, Color1, Color2, Color3;
        // color key positions in particle lifetime 0..1
        public float ColorKey1, ColorKey2;
        // alpha key positions in particle lifetime 0..1
        public float AlphaKey1, AlphaKey2;

        // internal value updated for shader by render system 
        // view space position
        public Vector3 Position;
        // bounce factor for colliding particles (0 - no bounce, 1 - same output velocity after bounce as input velocity)
        public float Bounciness;
        
        // emittor block shape size emitting from its volume
        public Vector3 PositionVariance;
        // transparency weighted function factor: to help accommodate for very bright particle systems shining from behind other particle systems
        // default: 1, lesser value will make the particle system "shine through" less
        public float OITWeightFactor;

        // direction and initial speed of the emitted particle
        public Vector3 Velocity;
        // random velocity variance
        public float VelocityVariance;

        // acceleration
        public Vector3 Acceleration;
        // rotation velocity
        public float RotationVelocity;

        // radius keys
        public float Size0, Size1, Size2, Size3;
        // radius key positions in particle lifetime 0..1
        public float SizeKeys1, SizeKeys2;
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
        public float __Pad1, __Pad2;

        public void InitDefaults()
        {
            Color0 = Color1 = Color2 = Color3 = Vector4.One;
            Size0 = Size1 = Size2 = Size3 = 1;
            ColorKey1 = ColorKey2 = 1.0f;
            AlphaKey1 = AlphaKey2 = 1.0f;
            Velocity = Vector3.Forward;
            ParticleLifeSpan = 1;
            SoftParticleDistanceScale = 1;
            RotationVelocity = 0;
            StreakMultiplier = 4.0f;
            AnimationFrameTime = 1.0f;
            OITWeightFactor = 1.0f;
            Bounciness = 0.5f;
        }
    }

    public struct MyGPUEmitter
    {
        // unique identifier of the emitter, so render system can pair emitted particles with their emitter over multiple frames
        public uint GID;
        // # of particles to emit per second
        public float ParticlesPerSecond;
        
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

    public struct MyGPUEmitterPositionUpdate
    {
        // unique identifier of the emitter, so render system can pair emitted particles with their emitter over multiple frames
        public uint GID;

        // world space position
        public Vector3D WorldPosition;
    }

    public class MyRenderMessageUpdateGPUEmitters : MyRenderMessageBase
    {
        public MyGPUEmitter[] Emitters = null;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmitters; } }
    }
}
