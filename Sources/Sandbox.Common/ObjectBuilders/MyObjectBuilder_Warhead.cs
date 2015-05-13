using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Warhead : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember(1)]
        public int CountdownMs = 10000;

        [ProtoMember(2)]
        public bool IsArmed = false;

        [ProtoMember(3)]
        public bool IsCountingDown = false;
    }
}
