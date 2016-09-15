namespace VRageRender.Messages
{
    public class MyRenderMessageSwitchDeviceSettings : MyRenderMessageBase
    {
        public MyRenderDeviceSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SwitchDeviceSettings; } }
    }
}
