using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

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

        [ProtoMember]
        public bool ShowInToolbarConfig = true;
    }
}
