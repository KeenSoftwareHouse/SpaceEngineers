using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SpaceBall : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float VirtualMass = 100;

        [ProtoMember]
        public float Friction = 0.5f;

        [ProtoMember]
        public float Restitution = 0.5f;

        [ProtoMember]
        public bool EnableBroadcast = true;
    }
}
