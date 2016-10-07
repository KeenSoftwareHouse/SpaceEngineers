namespace VRageRender.Messages
{
    public class MyRenderMessageUnloadTexture : MyRenderMessageBase
    {
        public string Texture;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UnloadTexture; } }
    }
}
