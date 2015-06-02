using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlTabPage : MyObjectBuilder_GuiControlParent
    {
        [ProtoMember]
        public int PageKey;

        [ProtoMember]
        public string TextEnum;
        
        [ProtoMember]
        public float TextScale;
    }
}
