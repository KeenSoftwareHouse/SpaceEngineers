using VRage.ObjectBuilders;
using ProtoBuf;
using VRage;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityGeneratorDefinition : MyObjectBuilder_GravityProviderDefinition
    {
        [ProtoMember]
        public float RequiredPowerInput;

        [ProtoMember]
        public SerializableBoundedVector3 FieldSize = new SerializableBoundedVector3(
            new SerializableVector3(0f, 0f, 0f),
            new SerializableVector3(150f, 150f, 150f),
            new SerializableVector3(150f, 150f, 150f)
        );
    }
}
