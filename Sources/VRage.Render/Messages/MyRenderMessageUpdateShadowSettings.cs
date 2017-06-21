namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateShadowSettings : MyRenderMessageBase
    {
        public MyRenderMessageUpdateShadowSettings()
        {
            Settings = new MyShadowsSettings();
        }

        public MyShadowsSettings Settings { get; private set; }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateShadowSettings; } }
    }
}
