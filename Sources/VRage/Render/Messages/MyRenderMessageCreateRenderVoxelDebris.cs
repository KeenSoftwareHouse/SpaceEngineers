using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateRenderVoxelDebris : IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public MatrixD WorldMatrix;
        public float TextureCoordOffset;
        public float TextureCoordScale;
        public float TextureColorMultiplier;
        public byte VoxelMaterialIndex;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateRenderVoxelDebris; } }
    }
}
