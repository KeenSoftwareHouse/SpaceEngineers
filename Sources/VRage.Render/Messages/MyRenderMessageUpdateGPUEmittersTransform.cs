using VRageMath;

namespace VRageRender.Messages
{
    public struct MyGPUEmitterTransformUpdate
    {
        // unique identifier of the emitter, so render system can pair emitted particles with their emitter over multiple frames
        public uint GID;

        // world space position
        public MatrixD Transform;
        // scale of emitter
        public float Scale;
        // gravity; world space
        public Vector3 Gravity;
        // # of particles to emit per second
        public float ParticlesPerSecond;
    }

    public class MyRenderMessageUpdateGPUEmittersTransform : MyRenderMessageBase
    {
        public MyGPUEmitterTransformUpdate[] Emitters;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmittersTransform; } }
    }
}
