using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public long IdentityId;
        [ProtoMember(2)]
        public List<MyObjectBuilder_PlayerChatHistory> PlayerChatHistory;
        [ProtoMember(3)]
        public MyObjectBuilder_GlobalChatHistory GlobalChatHistory;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlayerChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlArrayItem("PCI")]
        public List<MyObjectBuilder_PlayerChatItem> Chat;
        [ProtoMember(2)]
        [XmlElement(ElementName = "ID")]
        public long IdentityId;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlArrayItem("FCI")]
        public List<MyObjectBuilder_FactionChatItem> Chat;
        [ProtoMember(2)]
        [XmlElement(ElementName = "ID1")]
        public long FactionId1;
        [XmlElement(ElementName = "ID2")]
        public long FactionId2;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalChatHistory : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlArrayItem("GCI")]
        public List<MyObjectBuilder_GlobalChatItem> Chat;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlayerChatItem : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember(2)]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
        [ProtoMember(3)]
        [XmlElement(ElementName = "T")]
        public long TimestampMs;
        [ProtoMember(4), DefaultValue(true)]
        [XmlElement(ElementName = "S")]
        public bool Sent = true;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionChatItem : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember(2)]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
        [ProtoMember(3)]
        [XmlElement(ElementName = "T")]
        public long TimestampMs;
        [ProtoMember(4), DefaultValue(null)]
        [XmlElement(ElementName = "PTST")]
        public List<long> PlayersToSendToUniqueNumber;
        [ProtoMember(5), DefaultValue(null)]
        [XmlElement(ElementName = "IAST")]
        public List<bool> IsAlreadySentTo;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalChatItem : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        [XmlAttribute("t")]
        public string Text;
        [ProtoMember(2)]
        [XmlElement(ElementName = "I")]
        public long IdentityIdUniqueNumber;
    }
}