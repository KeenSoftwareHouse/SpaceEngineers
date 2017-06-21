using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateRenderObject : MyRenderMessageBase
    {
        public uint ID;
        public MatrixD WorldMatrix;
        public bool SortIntoCulling;
        public BoundingBoxD? AABB;
        public int LastMomentUpdateIndex=-1;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderObject; } }

        public override void Close()
        {
            AABB = null;
            base.Close();
        }
    }
}
