using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember]
        public long IdentityId;
        [ProtoMember]
        public List<MyObjectBuilder_PlayerChatHistory> PlayerChatHistory;
        [ProtoMember]
        public MyObjectBuilder_GlobalChatHistory GlobalChatHistory;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlayerChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlArrayItem("PCI")]
        public List<MyObjectBuilder_PlayerChatItem> Chat;
        [ProtoMember]
        [XmlElement(ElementName = "ID")]
        public long IdentityId;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlArrayItem("FCI")]
        public List<MyObjectBuilder_FactionChatItem> Chat;
        [ProtoMember]
        [XmlElement(ElementName = "ID1")]
        public long FactionId1;
        [XmlElement(ElementName = "ID2")]
        public long FactionId2;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlArrayItem("GCI")]
        public List<MyObjectBuilder_GlobalChatItem> Chat;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlayerChatItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
        [ProtoMember]
        [XmlElement(ElementName = "T")]
        public long TimestampMs;
        [ProtoMember, DefaultValue(true)]
        [XmlElement(ElementName = "S")]
        public bool Sent = true;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionChatItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
        [ProtoMember]
        [XmlElement(ElementName = "T")]
        public long TimestampMs;
        [ProtoMember, DefaultValue(null)]
        [XmlElement(ElementName = "PTST")]
        public List<long> PlayersToSendToUniqueNumber;
        [ProtoMember, DefaultValue(null)]
        [XmlElement(ElementName = "IAST")]
        public List<bool> IsAlreadySentTo;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalChatItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
        [ProtoMember]
        [XmlAttribute("a"), DefaultValue("")]
        public string Author;
        [ProtoMember]
        [XmlAttribute("f"), DefaultValue(MyFontEnum.Blue)]
        public string Font;
    }
}