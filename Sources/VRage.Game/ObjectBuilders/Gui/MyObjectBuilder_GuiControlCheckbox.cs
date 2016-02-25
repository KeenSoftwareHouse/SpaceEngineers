using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyGuiControlCheckboxStyleEnum
    {
        Default,
        Debug,
        SwitchOnOffLeft,
        SwitchOnOffRight,
        Repeat,
        Slave,
        Muted,
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