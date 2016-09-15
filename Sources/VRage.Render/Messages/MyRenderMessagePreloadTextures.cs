namespace VRageRender.Messages
{
    public class MyRenderMessagePreloadTextures : MyRenderMessageBase
    {
        public string InDirectory;
        public bool Recursive;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.PreloadTextures; } }

    }
}
