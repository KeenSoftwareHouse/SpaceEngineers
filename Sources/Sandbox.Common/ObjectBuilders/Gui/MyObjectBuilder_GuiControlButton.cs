using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
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
