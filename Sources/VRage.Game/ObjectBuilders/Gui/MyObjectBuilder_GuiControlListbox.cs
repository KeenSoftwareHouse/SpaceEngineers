using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyGuiControlListboxStyleEnum
    {
        Default,
        ContextMenu,
        Blueprints,
        ToolsBlocks,
        Terminal,
        IngameScipts,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlListbox : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public MyGuiControlListboxStyleEnum VisualStyle;

        [ProtoMember]
        public int VisibleRows;

    }
}