using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;

namespace VRageRender.Messages
{
    public struct MyClipmapCellMeshMetadata
    {
        public MyCellCoord Cell;
        public Vector3D PositionOffset;
        public Vector3 PositionScale;
        public BoundingBox LocalAabb;
        public int BatchCount;
    }

    public class MyRenderMessageUpdateClipmapCell : MyRenderMessageBase
    {
        public uint ClipmapId;
        public MyClipmapCellMeshMetadata Metadata;
        public List<MyClipmapCellBatch> Batches = new List<MyClipmapCellBatch>();

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateClipmapCell; } }

        public override void Close()
        {
            base.Close();
            Metadata = new MyClipmapCellMeshMetadata();
            Batches.Clear();
        }
    }
}
