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
        [ProtoMember(1)]
        public MyGuiControlGridStyleEnum VisualStyle;

        [ProtoMember(2)]
        public int DisplayColumnsCount = 1;

        [ProtoMember(3)]
        public int DisplayRowsCount = 1;
    }
}
