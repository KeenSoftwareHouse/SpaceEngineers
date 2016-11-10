using System;
using VRageMath;

namespace VRageRender.Messages
{
    public struct MyGPUEmitterLight
    {
        // unique identifier of the emitter, so render system can pair emitted particles with their emitter over multiple frames
        public uint GID;
        // # of particles to emit per second
        public float ParticlesPerSecond;
    }

    public class MyRenderMessageUpdateGPUEmittersLight : MyRenderMessageBase
    {
        public MyGPUEmitterLight[] Emitters = null;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmittersLight; } }
    }
}
