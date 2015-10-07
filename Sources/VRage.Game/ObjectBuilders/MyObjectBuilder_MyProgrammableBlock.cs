using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MyProgrammableBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string Program = null;

        [ProtoMember]
        public string Storage ="";

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string DefaultRunArgument = null;
    }
}
