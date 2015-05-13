using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    public enum MyGuiControlRadioButtonStyleEnum
    {
        FilterCharacter,
        FilterGrid,
        FilterAll,
        FilterEnergy,
        FilterStorage,
        FilterSystem,
        ScenarioButton,
        Rectangular
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlRadioButton : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember(1)]
        public int Key;

        [ProtoMember(2)]
        public MyGuiControlRadioButtonStyleEnum VisualStyle;
    }
}
