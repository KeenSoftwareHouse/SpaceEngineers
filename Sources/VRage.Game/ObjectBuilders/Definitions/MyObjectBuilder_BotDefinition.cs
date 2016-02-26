using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;
using System.ComponentModel;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BotDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class BotBehavior
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_BehaviorTreeDefinition);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public BotBehavior BotBehaviorTree;

        [ProtoMember, DefaultValue("")]
        public string BehaviorType = "";

        [ProtoMember, DefaultValue("")]
        public string BehaviorSubtype = "";

        [ProtoMember]
        public bool Commandable = false;
    }
}
