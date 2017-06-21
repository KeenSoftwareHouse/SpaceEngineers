
namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateGameplayFrame : MyRenderMessageBase
    {
        public int GameplayFrame;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateGameplayFrame; } }
    }
}