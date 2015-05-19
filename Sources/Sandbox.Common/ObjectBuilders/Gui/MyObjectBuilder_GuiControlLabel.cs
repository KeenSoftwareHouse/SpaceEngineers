using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlLabel : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public string TextEnum;
                
        [ProtoMember]
        public string Text;

        [ProtoMember]
        public float TextScale;

        [ProtoMember]
        public MyFontEnum Font;
    }
}
