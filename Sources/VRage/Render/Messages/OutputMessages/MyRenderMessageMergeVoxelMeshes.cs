using System;
using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateMergedVoxelMesh : MyRenderMessageBase
    {
        public uint ClipmapId;
        public int Lod;
        public ulong WorkId;
        public MyClipmapCellMeshMetadata Metadata;
        public readonly List<MyClipmapCellBatch> MergedBatches = new List<MyClipmapCellBatch>();

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateMergedVoxelMesh; } }

        public override void Close()
        {
            base.Close();
            MergedBatches.Clear();
        }
    }

    public class MyRenderMessageMergeVoxelMeshes : MyRenderMessageBase
    {
        public uint ClipmapId;
        public MyCellCoord CellCoord;
        public Func<int> Priority;
        public ulong WorkId;
        public readonly List<MyClipmapCellMeshMetadata> LodMeshMetadata = new List<MyClipmapCellMeshMetadata>();
        public readonly List<MyClipmapCellBatch> BatchesToMerge = new List<MyClipmapCellBatch>();

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.MergeVoxelMeshes; } }

        public override void Close()
        {
            base.Close();
            Priority = null;
            LodMeshMetadata.Clear();
            BatchesToMerge.Clear();
        }
    }

    public class MyRenderMessageCancelVoxelMeshMerge : MyRenderMessageBase
    {
        public uint ClipmapId;
        public ulong WorkId;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CancelVoxelMeshMerge; } }
    }

    public class MyRenderMessageResetMergedVoxels : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ResetMergedVoxels; } }
    }
}