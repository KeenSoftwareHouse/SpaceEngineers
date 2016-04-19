using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageRequestScreenData : MyRenderMessageBase
    {
        public int Id;
        public byte[] PreallocatedBuffer;
        public Vector2I Resolution;

        public override void Close()
        {
            Id = 0;
            PreallocatedBuffer = null;
            Resolution = new Vector2I();

            base.Close();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RequestScreenData; } }
    }
}