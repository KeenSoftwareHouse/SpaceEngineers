namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateSSAOSettings : MyRenderMessageBase
    {
        public MySSAOSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateSSAOSettings; } }
    }
}
