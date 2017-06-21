namespace VRageRender.Messages
{
    class MyRenderMessageClipmapsReady : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ClipmapsReady; } }
    }
}
