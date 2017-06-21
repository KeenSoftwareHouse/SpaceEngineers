using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TerminalBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string CustomName = null;

        [ProtoMember]
        public bool ShowOnHUD;

        [ProtoMember]
        public bool ShowInTerminal = true;

        [ProtoMember]
        public bool ShowInToolbarConfig = true;

        [ProtoMember]
        public bool ShowInInventory = true;
    }
}
