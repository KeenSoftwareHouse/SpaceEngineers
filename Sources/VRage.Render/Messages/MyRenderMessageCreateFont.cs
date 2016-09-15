namespace VRageRender.Messages
{
    public class MyRenderMessageCreateFont : MyRenderMessageBase
    {
        public int FontId;
        public string FontPath;
        public bool IsDebugFont;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateFont; } }
    }
}
