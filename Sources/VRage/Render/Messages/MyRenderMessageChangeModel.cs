
namespace VRageRender
{
    public class MyRenderMessageChangeModel : MyRenderMessageBase
    {
        public uint ID;
        public int LOD;
        public string Model;
        public bool UseForShadow;

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
