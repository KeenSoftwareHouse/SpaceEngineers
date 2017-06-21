using VRage;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateRenderVoxelMaterials : MyRenderMessageBase
    {
        public MyRenderVoxelMaterialData[] Materials;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderVoxelMaterials; } }
    }
}
