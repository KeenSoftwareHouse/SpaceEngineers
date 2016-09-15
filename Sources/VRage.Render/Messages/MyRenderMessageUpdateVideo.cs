namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateVideo : MyRenderMessageBase
    {
        public uint ID;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateVideo; } }
    }
}
