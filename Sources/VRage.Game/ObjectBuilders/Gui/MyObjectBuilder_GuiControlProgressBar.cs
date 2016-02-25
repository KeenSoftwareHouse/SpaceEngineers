using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlProgressBar : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public Vector4? ProgressColor;
        public bool ShouldSerializeProgressColor() { return ProgressColor.HasValue; }
    }
}
