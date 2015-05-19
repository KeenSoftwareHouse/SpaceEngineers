using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorRotor : MyObjectBuilder_CubeBlock
    {
        // We cannot save attached block entity IDs because copy/paste wouldn't work that way
        //[ProtoMember, DefaultValue(0)]
        //public long StatorEntityId = 0;
        //public bool ShouldSerializeStatorEntityId() { return StatorEntityId != 0; }
    }
}
