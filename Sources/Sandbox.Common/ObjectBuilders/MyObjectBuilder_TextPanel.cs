using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [Flags]
    public enum TextPanelAccessFlag : byte
    {
        NONE                        = 0,
        READ_FACTION                = (1 << 1),
        WRITE_FACTION               = (1 << 2),
        READ_AND_WRITE_FACTION      = (READ_FACTION | WRITE_FACTION),
        READ_ENEMY                  = (1 << 3),
        WRITE_ENEMY                 = (1 << 4),
        READ_ALL                    = READ_ENEMY | READ_FACTION,
        WRITE_ALL                   = WRITE_ENEMY | WRITE_FACTION,
        READ_AND_WRITE_ALL          = (READ_ALL | WRITE_ALL),
    }

    [Flags]
    public enum ShowTextOnScreenFlag : byte
    {
        NONE = 0,
        PUBLIC = (1 << 1),
        PRIVATE = (1 << 2),
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TextPanel : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public string Description = "";

        [ProtoMember]
        public string Title = "Title";

        [ProtoMember]
        public TextPanelAccessFlag AccessFlag = TextPanelAccessFlag.READ_AND_WRITE_FACTION;

        [ProtoMember]
        public float ChangeInterval = 0.0f;

        [ProtoMember]
        public List<string> SelectedImages = null;

        [ProtoMember]
        public float FontSize = 1.0f;

        [ProtoMember]
        public string PublicDescription = "";

        [ProtoMember]
        public string PublicTitle = "Public title";

        [ProtoMember]
        public ShowTextOnScreenFlag ShowText = ShowTextOnScreenFlag.NONE;

        [ProtoMember]
        public Color FontColor = Color.White;

        [ProtoMember]
        public Color BackgroundColor = Color.Black;

        [ProtoMember]
        public int CurrentShownTexture = 0;

    }
}
