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
        [ProtoMember]
        public Vector2 Position;
                
        [ProtoMember]
        public Vector2 Size;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public Vector4 BackgroundColor = Vector4.One;

        [ProtoMember]
        public string ControlTexture;

        [ProtoMember]
        public MyGuiDrawAlignEnum OriginAlign;

        public int ControlAlign
        {
            get { return (int)OriginAlign; }
            set { OriginAlign = (MyGuiDrawAlignEnum)value; }
        }
        public bool ShouldSerializeControlAlign() { return false; }
    }
}
