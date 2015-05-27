using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

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
