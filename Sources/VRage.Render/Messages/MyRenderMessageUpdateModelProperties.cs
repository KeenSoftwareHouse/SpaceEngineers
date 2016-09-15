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
        public Color? OutlineColor;
        public float OutlineThickness;
        public ulong PulseTimeInFrames;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateModelProperties; } }
    }
}
