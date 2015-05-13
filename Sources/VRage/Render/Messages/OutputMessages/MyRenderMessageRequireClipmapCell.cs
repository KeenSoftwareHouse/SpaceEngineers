using VRage.Voxels;

namespace VRageRender
{
    public class MyRenderMessageRequireClipmapCell : IMyRenderMessage
    {
        public uint ClipmapId;
        public MyCellCoord Cell;
        public bool HighPriority;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RequireClipmapCell; } }
    }

    public class MyRenderMessageCancelClipmapCell : IMyRenderMessage
    {
        public uint ClipmapId;
        public MyCellCoord Cell;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CancelClipmapCell; } }
    }
}
