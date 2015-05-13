using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeSequenceNode : MyObjectBuilder_BehaviorControlBaseNode
    {
    }
}
