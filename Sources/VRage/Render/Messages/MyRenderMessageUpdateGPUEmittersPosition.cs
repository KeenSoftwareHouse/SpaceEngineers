using System;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateGPUEmittersPosition : MyRenderMessageBase
    {
        public uint[] GIDs;
        public Vector3D[] WorldPositions;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmittersPosition; } }
    }
}
