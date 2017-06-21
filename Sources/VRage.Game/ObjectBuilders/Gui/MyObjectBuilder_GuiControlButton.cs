using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyGuiControlButtonStyleEnum
    {
        Default,
        Small,
        Red,
        Close,
        Info,
        InventoryTrash,
        Debug,
        ControlSetting,
        ClickableText,
        Increase,
        Decrease,
        Rectangular,
        Tiny,
        ArrowLeft,
        ArrowRight,
        Square,
        SquareSmall,
        UrlText,
        Error,
        Like,
        Envelope,
        Bug,
        Help
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlButton : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public string Text;

        [ProtoMember]
        public string TextEnum;

        [ProtoMember]
        public float TextScale;

        [ProtoMember]
        public int TextAlignment;

        [ProtoMember]
        public bool DrawCrossTextureWhenDisabled;

        [ProtoMember]
        public bool DrawRedTextureWhenDisabled;

        [ProtoMember]
        public MyGuiControlButtonStyleEnum VisualStyle;

    }
}