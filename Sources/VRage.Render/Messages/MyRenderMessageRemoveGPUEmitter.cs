namespace VRageRender.Messages
{
    public class MyRenderMessageRemoveGPUEmitter : MyRenderMessageBase
    {
        public uint GID;
        public bool Instant;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RemoveGPUEmitter; } }
    }
}
