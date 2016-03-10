using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageRequestScreenData : MyRenderMessageBase
    {
        public int Id;
        public byte[] PreallocatedBuffer;
        public Vector2 Resolution;

        public override void Close()
        {
            Id = 0;
            PreallocatedBuffer = null;
            Resolution = new Vector2();

            base.Close();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RequestScreenData; } }
    }
}