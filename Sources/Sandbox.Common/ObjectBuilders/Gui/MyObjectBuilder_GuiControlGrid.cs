using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    public enum MyGuiControlGridStyleEnum
    {
        Default,
        Toolbar,
        ToolsBlocks,
        ToolsWeapons,
        Inventory,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlGrid : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public MyGuiControlGridStyleEnum VisualStyle;

        [ProtoMember]
        public int DisplayColumnsCount = 1;

        [ProtoMember]
        public int DisplayRowsCount = 1;
    }
}
