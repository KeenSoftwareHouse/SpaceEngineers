
namespace VRageRender
{
    public class MyRenderMessageUpdateVoxelMaterialsProperties : IMyRenderMessage
    {
        public byte MaterialIndex;
        public float SpecularPower;
        public float SpecularIntensity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateVoxelMaterialsProperties; } }
    }
}
