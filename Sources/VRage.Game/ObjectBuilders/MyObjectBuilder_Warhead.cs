using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Warhead : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public int CountdownMs = 10000;

        [ProtoMember]
        public bool IsArmed = false;

        [ProtoMember]
        public bool IsCountingDown = false;
    }
}
