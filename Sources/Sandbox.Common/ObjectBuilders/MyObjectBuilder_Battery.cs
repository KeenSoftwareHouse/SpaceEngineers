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
        [ProtoMember(1), DefaultValue(true)]
        public bool ProducerEnabled = true;

        [ProtoMember(2)]
        public float CurrentCapacity;
    }
}
