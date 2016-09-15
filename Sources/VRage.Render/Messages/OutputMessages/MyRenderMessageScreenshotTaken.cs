
namespace VRageRender.Messages
{
    public class MyRenderMessageScreenshotTaken : MyRenderMessageBase
    {
        public bool Success;
        public string Filename;
        public bool ShowNotification;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ScreenshotTaken; } }
    }
}
