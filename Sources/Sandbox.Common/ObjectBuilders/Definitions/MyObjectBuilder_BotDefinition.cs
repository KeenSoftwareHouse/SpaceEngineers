using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
            [ProtoMember(1)]
            public string Subtype;
        }

        [ProtoMember(1)]
        public BotBehavior BotBehaviorTree;

        [ProtoMember(2), DefaultValue("")]
        public string BehaviorType = "";

        [ProtoMember(3), DefaultValue("")]
        public string BehaviorSubtype = "";
    }
}
