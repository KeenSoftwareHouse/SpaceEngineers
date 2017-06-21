namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateMaterialsSettings : MyRenderMessageBase
    {
        public MyRenderMessageUpdateMaterialsSettings()
        {
            Settings = new MyMaterialsSettings();
        }

        public MyMaterialsSettings Settings { get; private set; }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateMaterialsSettings; } }
    }
}
