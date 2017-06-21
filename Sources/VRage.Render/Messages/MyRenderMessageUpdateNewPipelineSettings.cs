namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateNewPipelineSettings : MyRenderMessageBase
    {
        public MyRenderMessageUpdateNewPipelineSettings()
        {
            Settings = new MyNewPipelineSettings();
        }

        public MyNewPipelineSettings Settings { get; private set; }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateNewPipelineSettings; } }
    }
}
