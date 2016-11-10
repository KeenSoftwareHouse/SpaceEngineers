using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
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
        [Serialize(MyObjectFlags.Nullable)]
        public List<string> SelectedImages = null;

        [ProtoMember]
        public SerializableDefinitionId Font = new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), "Debug");

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
