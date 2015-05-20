using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
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
