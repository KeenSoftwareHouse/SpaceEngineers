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
        [ProtoMember(1)]
        public string Text;

        [ProtoMember(2)]
        public string TextEnum;

        [ProtoMember(5)]
        public float TextScale;

        [ProtoMember(6)]
        public int TextAlignment;

        [ProtoMember(7)]
        public bool DrawCrossTextureWhenDisabled;

        [ProtoMember(10)]
        public bool DrawRedTextureWhenDisabled;

        [ProtoMember(12)]
        public MyGuiControlButtonStyleEnum VisualStyle;

    }
}
