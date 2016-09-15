using System;
using VRage.Voxels;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageRequireClipmapCell : MyRenderMessageBase
    {
        public uint ClipmapId;
        public MyCellCoord Cell;
        public Func<int> Priority;
        public Action<Color> DebugDraw;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RequireClipmapCell; } }

        public override void Close() {
            base.Close();
            ClipmapId = uint.MaxValue;
            Priority = null;
            DebugDraw = null;
        }
    }
}
