using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
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
        Rectangular,
        Custom
    }

    [ProtoContract]
    public struct MyGuiCustomVisualStyle
    {
        [ProtoMember]
        public string NormalTexture;
        [ProtoMember]
        public string HighlightTexture;
        [ProtoMember]
        public Vector2 Size;
        [ProtoMember]
        public string NormalFont;
        [ProtoMember]
        public string HighlightFont;
        [ProtoMember]
        public float HorizontalPadding;
        [ProtoMember]
        public float VerticalPadding;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlRadioButton : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public int Key;

        [ProtoMember]
        public MyGuiControlRadioButtonStyleEnum VisualStyle;

        /// <summary>
        /// Custom visual style. This is check if visual style is set to Custom.
        /// </summary>
        [ProtoMember]
        public MyGuiCustomVisualStyle? CustomVisualStyle = null;

    }
}