#region Using

using ProtoBuf;
using VRageMath;
using VRage.ObjectBuilders;

#endregion

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiScreen : MyObjectBuilder_Base
    {
        [ProtoMember]
        public MyObjectBuilder_GuiControls Controls;

        [ProtoMember]
        public Vector4? BackgroundColor;

        [ProtoMember]
        public string BackgroundTexture;

        [ProtoMember]
        public Vector2? Size;

        [ProtoMember]
        public bool CloseButtonEnabled;

        [ProtoMember]
        public Vector2 CloseButtonOffset;
        public bool ShouldSerializeCloseButtonOffset() { return CloseButtonEnabled; }
    }
}
