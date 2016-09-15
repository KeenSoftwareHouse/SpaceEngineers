namespace VRageRender.Messages
{
    public class MyRenderMessageRenderTextureFreed : MyRenderMessageBase
    {
        public int FreeResources;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RenderTextureFreed; } }
    }

}
