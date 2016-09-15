namespace VRageRender.Messages
{
    class MyRenderMessageDebugPrintAllFileTexturesIntoLog : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugPrintAllFileTexturesIntoLog; } }
    }
}
