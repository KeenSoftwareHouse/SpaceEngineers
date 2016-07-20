using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateModelHighlight : MyRenderMessageBase
    {
        public uint ID;
        public int InstanceIndex;
        public string Model;
        public int[] SectionIndices;
        public uint[] SubpartIndices;
        public Color? OutlineColor;
        public float Thickness;
        public ulong PulseTimeInFrames;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateModelHighlight; } }
    }
}
