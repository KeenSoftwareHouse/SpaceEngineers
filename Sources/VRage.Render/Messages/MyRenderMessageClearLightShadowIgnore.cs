namespace VRageRender.Messages
{
    public class MyRenderMessageClearLightShadowIgnore : MyRenderMessageBase
    {
        public uint ID;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ClearLightShadowIgnore; } }
    }
}
