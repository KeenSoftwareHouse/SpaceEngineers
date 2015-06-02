using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MyProgrammableBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public string Program = null;

        [ProtoMember]
        public string Storage ="";

        [ProtoMember]
        public string DefaultRunArgument = null;
    }
}
