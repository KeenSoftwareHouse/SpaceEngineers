using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateModelProperties : MyRenderMessageBase
    {
        public uint ID;
        public int LOD;
        public int MeshIndex;
        public string MaterialName;
        public bool? Enabled;
        public Color? DiffuseColor;
        public float? Emissivity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateModelProperties; } }
    }
}
