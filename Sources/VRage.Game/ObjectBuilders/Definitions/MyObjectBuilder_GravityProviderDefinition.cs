using ProtoBuf;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityProviderDefinition : MyObjectBuilder_CubeBlockDefinition
    {

        [ProtoMember]
        public SerializableBounds Gravity = new SerializableBounds(-1f, 1f, 1f);
    }
}
