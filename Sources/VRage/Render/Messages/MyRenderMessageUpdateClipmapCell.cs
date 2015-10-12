using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    public struct MyClipmapCellMeshMetadata
    {
        public MyCellCoord Cell;
        public Vector3D PositionOffset;
        public Vector3 PositionScale;
        public BoundingBox LocalAabb;
    }

    public class MyRenderMessageUpdateClipmapCell : IMyRenderMessage
    {
        public uint ClipmapId;
        public MyClipmapCellMeshMetadata Metadata;
        public readonly List<MyClipmapCellBatch> Batches = new List<MyClipmapCellBatch>();

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateClipmapCell; } }
    }
}
