using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Common.Import;
using VRage.Common.Utils;

namespace VRageRender
{
    public class MyRenderMessageCreateRenderVoxelCell : IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public int VoxelID;
        public Vector3I Coord;
        public BoundingBoxD AABB;
        public Vector3D PositionLeftBottomCorner;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateRenderVoxelCell; } }
    }
}
