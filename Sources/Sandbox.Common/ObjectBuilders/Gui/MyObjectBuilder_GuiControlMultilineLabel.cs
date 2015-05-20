using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlMultilineLabel : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public float TextScale = 1.0f;

        [ProtoMember]
        public int TextAlign;

        [ProtoMember]
        public Vector4 TextColor = Vector4.One;

        [ProtoMember]
        public string Text;

        [ProtoMember]
        public int TextBoxAlign;

        [ProtoMember]
        public MyFontEnum Font;
    }
}
