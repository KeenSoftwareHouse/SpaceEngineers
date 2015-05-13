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
        [ProtoMember(1)]
        public bool Open = true;

        [ProtoMember(2)]
        public float CurrOpening = 1f;
    }
}
