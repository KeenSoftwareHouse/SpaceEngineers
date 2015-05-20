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
    public class MyObjectBuilder_BehaviorTreeControlNodeMemory : MyObjectBuilder_BehaviorTreeNodeMemory
    {
        [XmlAttribute]
        [ProtoMember, DefaultValue(0)]
        public int InitialIndex = 0;
    }
}
