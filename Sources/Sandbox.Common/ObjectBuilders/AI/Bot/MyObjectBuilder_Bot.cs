using ProtoBuf;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Bot : MyObjectBuilder_Base
    {
        [ProtoMember]
        public SerializableDefinitionId BotDefId;

        [ProtoMember]
        public MyObjectBuilder_BotMemory BotMemory;

        [ProtoMember]
        public string LastBehaviorTree = null;
    }
}
