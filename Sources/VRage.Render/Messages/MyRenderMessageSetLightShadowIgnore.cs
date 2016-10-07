namespace VRageRender.Messages
{
    public class MyRenderMessageSetLightShadowIgnore : MyRenderMessageBase
    {
        public uint ID;
        public uint ID2;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetLightShadowIgnore; } }
    }
}
