namespace VRageRender.Messages
{
    public class MyRenderMessageUpdatePostprocessSettings : MyRenderMessageBase
    {
        public MyPostprocessSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdatePostprocessSettings; } }
    }
}
