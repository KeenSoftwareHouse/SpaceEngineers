
namespace VRageRender
{
    public class MyRenderMessageChangeModel : IMyRenderMessage
    {
        public uint ID;
        public int LOD;
        public string Model;
        public bool UseForShadow;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ChangeModel; } }
    }

    public class MyRenderMessageChangeModelMaterial : IMyRenderMessage
    {
        public string Model;
        public string Material;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.ChangeModelMaterial; } }
    }
}
