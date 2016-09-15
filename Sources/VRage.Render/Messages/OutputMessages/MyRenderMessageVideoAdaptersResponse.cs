namespace VRageRender.Messages
{
    public class MyRenderMessageVideoAdaptersResponse : MyRenderMessageBase
    {
        public MyAdapterInfo[] Adapters;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.VideoAdaptersResponse; } }
    }
}
