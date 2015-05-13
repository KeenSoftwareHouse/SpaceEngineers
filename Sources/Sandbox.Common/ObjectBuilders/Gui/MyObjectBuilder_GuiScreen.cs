#region Using

using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

#endregion

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiScreen : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public MyObjectBuilder_GuiControls Controls;

        [ProtoMember(2)]
        public Vector4? BackgroundColor;

        [ProtoMember(3)]
        public string BackgroundTexture;

        [ProtoMember(4)]
        public Vector2? Size;

        [ProtoMember(5)]
        public bool CloseButtonEnabled;

        [ProtoMember(7)]
        public Vector2 CloseButtonOffset;
        public bool ShouldSerializeCloseButtonOffset() { return CloseButtonEnabled; }
    }
}
