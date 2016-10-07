namespace VRageRender.Messages
{    
    public class MyRenderMessagePreloadModel : MyRenderMessageBase
    {
        public string Name;
        public float Rescale = 1.0f;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.PreloadModel; } }
    }
}
