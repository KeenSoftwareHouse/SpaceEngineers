using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageSetInstanceBuffer : MyRenderMessageBase
    {
        public uint ID;
        public uint InstanceBufferId;
        public int InstanceStart;
        public int InstanceCount;
        public BoundingBox LocalAabb;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetInstanceBuffer; } }
    }
}
