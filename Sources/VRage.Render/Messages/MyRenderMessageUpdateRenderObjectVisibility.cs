namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateRenderObjectVisibility : MyRenderMessageBase
    {
        public uint ID;
        public bool Visible; //Note that invisible objects still can cast shadows
        public bool NearFlag;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderObjectVisibility; } }
    }
}
