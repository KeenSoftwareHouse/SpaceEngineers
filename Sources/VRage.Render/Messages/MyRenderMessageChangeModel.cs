
namespace VRageRender.Messages
{
    public class MyRenderMessageChangeModel : MyRenderMessageBase
    {
        public uint ID;
        public string Model;
        public float Scale;

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
