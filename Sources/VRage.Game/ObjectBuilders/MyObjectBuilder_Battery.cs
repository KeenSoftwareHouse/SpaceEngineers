using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Battery : MyObjectBuilder_Base
    {
        [ProtoMember, DefaultValue(true)]
        public bool ProducerEnabled = true;

        [ProtoMember]
        public float CurrentCapacity;
    }
}
