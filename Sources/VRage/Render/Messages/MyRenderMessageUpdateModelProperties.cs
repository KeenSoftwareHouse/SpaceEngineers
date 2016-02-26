using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateModelProperties : MyRenderMessageBase
    {
        public uint ID;
        public int LOD;
        public string Model;
        public int MeshIndex;
        public string MaterialName;
        public bool? Enabled;
        public Color? DiffuseColor;
        public float? SpecularPower;
        public float? SpecularIntensity;
        public float? Emissivity;
        public Color? OutlineColor;
        public float OutlineThickness;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateModelProperties; } }
    }
}
