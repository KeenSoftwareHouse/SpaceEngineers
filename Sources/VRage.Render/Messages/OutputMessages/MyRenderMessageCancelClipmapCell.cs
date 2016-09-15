using VRage.Voxels;

namespace VRageRender.Messages
{
    public class MyRenderMessageCancelClipmapCell : MyRenderMessageBase
    {
        public uint ClipmapId;
        public MyCellCoord Cell;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CancelClipmapCell; } }
    }
}
