using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateGPUEmittersTransform : MyRenderMessageBase
    {
        public uint[] GIDs;
        public MatrixD[] Transforms;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGPUEmittersTransform; } }
    }
}
