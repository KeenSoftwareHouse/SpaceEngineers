namespace VRageRender.Messages
{
    public class MyRenderMessageResetRandomness : MyRenderMessageBase
    {
        public int? Seed;

        public override MyRenderMessageType MessageClass
        {
            get { return MyRenderMessageType.StateChangeOnce; }
        }

        public override MyRenderMessageEnum MessageType
        {
            get { return MyRenderMessageEnum.ResetRandomness; }
        }
    }
}
