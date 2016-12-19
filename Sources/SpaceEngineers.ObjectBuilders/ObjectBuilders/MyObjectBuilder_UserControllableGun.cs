using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_UserControllableGun : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsShooting = false;

        [ProtoMember]
        public bool IsShootingFromTerminal = false;

        [ProtoMember]
        public bool IsLargeTurret = false;

        [ProtoMember]
        public float MinFov = 0.1f;

        [ProtoMember]
        public float MaxFov = 1.04719755f;

    }
}
