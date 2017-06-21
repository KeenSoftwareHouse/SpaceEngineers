namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateNewLoddingSettings : MyRenderMessageBase
    {
        public MyRenderMessageUpdateNewLoddingSettings()
        {
            Settings = new MyNewLoddingSettings();
        }

        public MyNewLoddingSettings Settings { get; private set; }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateNewLoddingSettings; } }
    }
}
