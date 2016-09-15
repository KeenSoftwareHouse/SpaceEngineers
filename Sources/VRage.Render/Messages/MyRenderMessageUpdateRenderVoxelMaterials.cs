using VRage;

namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateRenderVoxelMaterials : MyRenderMessageBase
    {
        public MyRenderVoxelMaterialData[] Materials;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderVoxelMaterials; } }
    }
}
