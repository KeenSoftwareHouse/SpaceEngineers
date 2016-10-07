using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGeneratorSphereDefinition : MyObjectBuilder_GravityGeneratorBaseDefinition
    {
        [ProtoMember]
        public float MinRadius;
        [ProtoMember]
        public float MaxRadius;
        [ProtoMember]
        public float BasePowerInput;
        [ProtoMember]
        public float ConsumptionPower;
    }
}
