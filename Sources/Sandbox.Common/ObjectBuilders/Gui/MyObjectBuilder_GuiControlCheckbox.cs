using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    public enum MyGuiControlCheckboxStyleEnum
    {
        Default,
        Debug,
        SwitchOnOffLeft,
        SwitchOnOffRight,
        Repeat,
        Slave,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlCheckbox : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember(1)]
        public bool IsChecked;
                
        [ProtoMember(2)]
        public string CheckedTexture;

        [ProtoMember(3)]
        public MyGuiControlCheckboxStyleEnum VisualStyle;

    }
}
