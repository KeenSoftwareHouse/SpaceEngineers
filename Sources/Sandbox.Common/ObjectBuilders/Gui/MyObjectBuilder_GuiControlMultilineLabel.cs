using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlMultilineLabel : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember(1)]
        public float TextScale = 1.0f;

        [ProtoMember(2)]
        public int TextAlign;

        [ProtoMember(3)]
        public Vector4 TextColor = Vector4.One;

        [ProtoMember(4)]
        public string Text;

        [ProtoMember(5)]
        public int TextBoxAlign;

        [ProtoMember(6)]
        public MyFontEnum Font;
    }
}
