using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Definitions;
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
    public class MyObjectBuilder_BehaviorTreeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeNode FirstNode;

        // MW:TODO remove (use masks)
        [ProtoMember, DefaultValue("Barbarian")]
        public string Behavior = "Barbarian";
    }
}
