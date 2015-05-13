using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageInvalidateClipmapRange : IMyRenderMessage
    {
        public uint ClipmapId;
        public Vector3I MinCellLod0;
        public Vector3I MaxCellLod0;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.InvalidateClipmapRange; } }
    }
}
