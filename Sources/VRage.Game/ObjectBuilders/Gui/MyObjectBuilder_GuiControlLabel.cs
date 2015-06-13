using ProtoBuf;
using System;
using VRageMath;
using VRage.ObjectBuilders;

namespace Sandbox.Common
{
    public enum MyFontEnum
    {
        Debug, // First font will be used as debug by engine, so we put Debug font here.
        Red,
        Green,
        Blue,
        White,
        DarkBlue,

        UrlNormal,
        UrlHighlight,
        ErrorMessageBoxCaption,
        ErrorMessageBoxText,
        InfoMessageBoxCaption,
        InfoMessageBoxText,
        ScreenCaption,
        GameCredits,
        LoadingScreen,

        BuildInfo,
        BuildInfoHighlight,
    }
}

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
