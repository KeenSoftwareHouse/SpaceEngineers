using VRage.ObjectBuilders;
using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlParent : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public MyObjectBuilder_GuiControls Controls;

    }
}
