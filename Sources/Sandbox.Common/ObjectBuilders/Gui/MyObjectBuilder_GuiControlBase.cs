using ProtoBuf;
using System;
using VRageMath;
using VRage;
using VRage.Utils;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_GuiControlBase : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public Vector2 Position;
                
        [ProtoMember(2)]
        public Vector2 Size;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public Vector4 BackgroundColor = Vector4.One;

        [ProtoMember(5)]
        public string ControlTexture;

        [ProtoMember(6)]
        public MyGuiDrawAlignEnum OriginAlign;

        public int ControlAlign
        {
            get { return (int)OriginAlign; }
            set { OriginAlign = (MyGuiDrawAlignEnum)value; }
        }
        public bool ShouldSerializeControlAlign() { return false; }
    }
}
