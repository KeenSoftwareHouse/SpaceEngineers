namespace VRageRender.Messages
{
    public class MyRenderMessageEnableAtmosphere : MyRenderMessageBase
    {
        public bool Enabled;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.EnableAtmosphere; } }
    }
}
