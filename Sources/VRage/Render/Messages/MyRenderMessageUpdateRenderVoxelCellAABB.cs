
using VRageMath;
using VRageRender.Graphics;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderVoxelCellAABB : IMyRenderMessage
    {
        public uint ID;
        public BoundingBoxD AABB;
        public Vector3D PositionLeftBottomCorner;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderVoxelCellAABB; } }
    }
}
