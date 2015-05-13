
namespace VRageRender
{
    public class MyRenderMessageScreenshotTaken : IMyRenderMessage
    {
        public bool Success;
        public string Filename;
        public bool ShowNotification;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ScreenshotTaken; } }
    }
}
