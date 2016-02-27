using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateModelHighlight : MyRenderMessageBase
    {
        public uint ID;
        public int LOD;
        public string Model;
        public int[] SectionIndices;
        public string MaterialName;
        public Color? OutlineColor;
        public float Thickness;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateModelHighlight; } }
    }
}
