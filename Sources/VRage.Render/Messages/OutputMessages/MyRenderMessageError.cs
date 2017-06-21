
namespace VRageRender.Messages
{
    public class MyRenderMessageError : MyRenderMessageBase
    {
        public string Callstack;
        public string Message;
        public bool ShouldTerminate;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.Error; } }
    }
}
