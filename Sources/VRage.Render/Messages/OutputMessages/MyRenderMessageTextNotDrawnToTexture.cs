namespace VRageRender.Messages
{
    public class MyRenderMessageTextNotDrawnToTexture : MyRenderMessageBase
    {
        public long EntityId;
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.TextNotDrawnToTexture; } }
    }
}
