using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FunctionalBlock : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public bool Enabled = true; // Can by overriden by subclasses in constructor, which is why there is no attribute DefaultValue.
    }
}
