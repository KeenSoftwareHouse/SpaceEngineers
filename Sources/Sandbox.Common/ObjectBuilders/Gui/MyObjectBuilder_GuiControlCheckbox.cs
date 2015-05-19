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
        [ProtoMember]
        public bool IsChecked;
                
        [ProtoMember]
        public string CheckedTexture;

        [ProtoMember]
        public MyGuiControlCheckboxStyleEnum VisualStyle;

    }
}
