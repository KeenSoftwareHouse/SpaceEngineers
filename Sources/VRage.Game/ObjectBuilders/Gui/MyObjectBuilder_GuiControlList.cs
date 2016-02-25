using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyGuiControlListStyleEnum
    {
        Default,
        Simple,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlList : MyObjectBuilder_GuiControlParent
    {
        [ProtoMember]
        public MyGuiControlListStyleEnum VisualStyle;
    }
}