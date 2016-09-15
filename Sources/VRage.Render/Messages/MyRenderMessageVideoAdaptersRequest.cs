namespace VRageRender.Messages
{
    class MyRenderMessageVideoAdaptersRequest : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.VideoAdaptersRequest; } }
    }
}
