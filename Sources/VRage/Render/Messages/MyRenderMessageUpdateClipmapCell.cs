using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateClipmapCell : IMyRenderMessage
    {
        public uint ClipmapId;
        public MyCellCoord Cell;
        public readonly List<MyClipmapCellBatch> Batches = new List<MyClipmapCellBatch>();
        public Vector3D PositionOffset;
        public Vector3 PositionScale;
        public BoundingBox MeshAabb;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateClipmapCell; } }
    }
}
