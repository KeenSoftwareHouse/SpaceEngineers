using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlLabel : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember(1)]
        public string TextEnum;
                
        [ProtoMember(2)]
        public string Text;

        [ProtoMember(4)]
        public float TextScale;

        [ProtoMember(5)]
        public MyFontEnum Font;
    }
}
