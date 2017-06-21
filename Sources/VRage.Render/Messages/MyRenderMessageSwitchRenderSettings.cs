namespace VRageRender.Messages
{
    /// <summary>
    /// 1 at the end is naming convention from DX, saying this is newer version (for Dx11 render).
    /// </summary>
    public class MyRenderMessageSwitchRenderSettings : MyRenderMessageBase
    {
        public MyRenderSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SwitchRenderSettings; } }
    }
}
