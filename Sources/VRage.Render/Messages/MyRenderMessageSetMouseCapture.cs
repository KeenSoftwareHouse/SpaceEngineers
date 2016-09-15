namespace VRageRender.Messages
{
    public class MyRenderMessageSetMouseCapture : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetMouseCapture; } }

        public bool Capture;
    }
}
