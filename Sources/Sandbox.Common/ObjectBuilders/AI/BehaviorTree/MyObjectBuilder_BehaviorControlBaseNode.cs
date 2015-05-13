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
        [ProtoMember(1)]
        public MyObjectBuilder_BehaviorTreeNode[] BTNodes = null;

        [ProtoMember(2)]
        public string Name = null;

        [ProtoMember(3), DefaultValue(false)]
        public bool IsMemorable = false;
    }
}
