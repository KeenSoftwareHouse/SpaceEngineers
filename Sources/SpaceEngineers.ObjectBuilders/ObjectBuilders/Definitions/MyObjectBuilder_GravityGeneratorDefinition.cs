using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;
using VRage;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGeneratorDefinition : MyObjectBuilder_GravityGeneratorBaseDefinition
    {
        [ProtoMember]
        public float RequiredPowerInput;
        [ProtoMember]
        public SerializableVector3 MinFieldSize = new SerializableVector3(1, 1, 1);
        [ProtoMember]
        public SerializableVector3 MaxFieldSize = new SerializableVector3(150, 150, 150);
    }
}
