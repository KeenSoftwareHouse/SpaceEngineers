using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageInvalidateClipmapRange : MyRenderMessageBase
    {
        public uint ClipmapId;
        public Vector3I MinCellLod0;
        public Vector3I MaxCellLod0;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.InvalidateClipmapRange; } }
    }
}
