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
        [ProtoMember, DefaultValue(null)]
        public string CustomName = null;

        [ProtoMember]
        public bool ShowOnHUD;

        [ProtoMember]
        public bool ShowInTerminal = true;
    }
}
