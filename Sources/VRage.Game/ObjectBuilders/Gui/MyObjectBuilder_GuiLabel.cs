using ProtoBuf;
using System;
using VRage.CommonLib.Utils;
using VRageMath;

namespace Sandbox.CommonLib.ObjectBuilders
{
    [ProtoContract, ProtoDynamicInheritance]
    [MyObjectBuilderDefinition(MyObjectBuilderTypeEnum.GuiLabel)]
    public class MyObjectBuilder_GuiLabel : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember(1)]
        public int TextEnum;
                
        [ProtoMember(2)]
        public string Text;

        [ProtoMember(3)]
        public Vector4 TextColor;

        [ProtoMember(4)]
        public float TextScale;

        [ProtoMember(5)]
        public int TextAlign;


        internal MyObjectBuilder_GuiLabel()
        {
        }
    }
}
