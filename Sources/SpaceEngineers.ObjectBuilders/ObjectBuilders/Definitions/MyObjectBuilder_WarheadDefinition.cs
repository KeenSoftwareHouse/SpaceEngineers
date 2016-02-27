using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_WarheadDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float ExplosionRadius = 0.0f;

        [ProtoMember]
        public float WarheadExplosionDamage = 15000f;
    }
}
