using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateRenderVoxelDebris : MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public MatrixD WorldMatrix;
        public float TextureCoordOffset;
        public float TextureCoordScale;
        public float TextureColorMultiplier;
        public byte VoxelMaterialIndex;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderVoxelDebris; } }
    }
}
