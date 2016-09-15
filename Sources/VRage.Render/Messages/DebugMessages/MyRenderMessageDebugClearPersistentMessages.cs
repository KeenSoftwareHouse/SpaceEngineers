namespace VRageRender.Messages
{
    public class MyRenderMessageDebugClearPersistentMessages : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.DebugDraw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugClearPersistentMessages; } }
    }
}
