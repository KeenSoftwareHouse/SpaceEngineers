using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalEventBase : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public SerializableDefinitionId DefinitionId;

        //[ProtoMember(2)]
        //public bool WriteToLog;

        [ProtoMember(3)]
        public bool Enabled;

        [ProtoMember(4)]
        public long ActivationTimeMs;

        [ProtoMember(5)] // Obsolete, use DefinitionId now!
        public MyGlobalEventTypeEnum EventType;
        public bool ShouldSerializeEventType() { return false; }
    }
}
