using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WarheadDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float ExplosionRadius = 0.0f;
    }
}
