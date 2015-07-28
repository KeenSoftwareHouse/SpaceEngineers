using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalEventBase : MyObjectBuilder_Base
    {
        // Obsolete!
        [ProtoMember]
        public SerializableDefinitionId? DefinitionId = null;
        public bool ShouldSerializeDefinitionId() { return false; }

        //[ProtoMember]
        //public bool WriteToLog;

        [ProtoMember]
        public bool Enabled;

        [ProtoMember]
        public long ActivationTimeMs;

        [ProtoMember] // Obsolete, use DefinitionId now!
        public MyGlobalEventTypeEnum EventType;
        public bool ShouldSerializeEventType() { return false; }
    }
}
