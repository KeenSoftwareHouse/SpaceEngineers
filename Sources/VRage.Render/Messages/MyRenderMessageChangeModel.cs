
namespace VRageRender.Messages
{
    public class MyRenderMessageChangeModel : MyRenderMessageBase
    {
        public uint ID;
        public int LOD;
        public string Model;
        public bool UseForShadow;
        public float Rescale = 1.0f;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ChangeModel; } }
    }

    public class MyRenderMessageChangeModelMaterial : MyRenderMessageBase
    {
        public string Model;
        public string Material;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ChangeModelMaterial; } }
    }
}
