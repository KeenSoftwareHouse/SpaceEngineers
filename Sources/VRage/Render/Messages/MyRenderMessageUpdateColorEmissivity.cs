using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateColorEmissivity : IMyRenderMessage
    {
        public uint ID;
        public int LOD;
        public string MaterialName;
        public Color DiffuseColor;
        public float Emissivity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateColorEmissivity; } }
    }
}
