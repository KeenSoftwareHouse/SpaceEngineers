using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateManualCullObject: MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public MatrixD WorldMatrix;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateManualCullObject; } }
    }
}
