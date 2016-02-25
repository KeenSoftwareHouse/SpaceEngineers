
namespace VRageRender
{
    public class MyRenderMessageUpdateVoxelMaterialsProperties : MyRenderMessageBase
    {
        public byte MaterialIndex;
        public float SpecularPower;
        public float SpecularIntensity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateVoxelMaterialsProperties; } }
    }
}
