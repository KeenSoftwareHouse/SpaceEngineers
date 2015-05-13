using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TerminalBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember(1), DefaultValue(null)]
        public string CustomName = null;

        [ProtoMember(2)]
        public bool ShowOnHUD;

        [ProtoMember(3)]
        public bool ShowInTerminal = true;
    }
}
