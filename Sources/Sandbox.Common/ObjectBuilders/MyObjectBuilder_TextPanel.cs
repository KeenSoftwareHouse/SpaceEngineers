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
        [ProtoMember(1)]
        public string Description = "";

        [ProtoMember(2)]
        public string Title = "Title";

        [ProtoMember(3)]
        public TextPanelAccessFlag AccessFlag = TextPanelAccessFlag.READ_AND_WRITE_FACTION;

        [ProtoMember(4)]
        public float ChangeInterval = 0.0f;

        [ProtoMember(5)]
        public List<string> SelectedImages = null;

        [ProtoMember(6)]
        public float FontSize = 1.0f;

        [ProtoMember(7)]
        public string PublicDescription = "";

        [ProtoMember(8)]
        public string PublicTitle = "Public title";

        [ProtoMember(9)]
        public ShowTextOnScreenFlag ShowText = ShowTextOnScreenFlag.NONE;

        [ProtoMember(10)]
        public Color FontColor = Color.White;

        [ProtoMember(11)]
        public Color BackgroundColor = Color.Black;

        [ProtoMember(12)]
        public int CurrentShownTexture = 0;

        [ProtoMember(13)]
        public MyFontEnum FontFace = MyFontEnum.Debug;

    }
}
