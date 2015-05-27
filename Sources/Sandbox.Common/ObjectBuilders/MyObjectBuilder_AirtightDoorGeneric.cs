using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirtightDoorGeneric : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool Open = true;

        [ProtoMember]
        public float CurrOpening = 1f;
    }
}
