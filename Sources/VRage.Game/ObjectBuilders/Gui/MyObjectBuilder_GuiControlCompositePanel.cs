using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlCompositePanel : MyObjectBuilder_GuiControlPanel
    {
        [ProtoMember]
        public float InnerHeight;

    }
}
