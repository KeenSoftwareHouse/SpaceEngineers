using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorControlBaseNode : MyObjectBuilder_BehaviorTreeNode
    {
        [XmlArrayItem("BTNode")]
        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeNode[] BTNodes = null;

        [ProtoMember]
        public string Name = null;

        [ProtoMember, DefaultValue(false)]
        public bool IsMemorable = false;
    }
}
