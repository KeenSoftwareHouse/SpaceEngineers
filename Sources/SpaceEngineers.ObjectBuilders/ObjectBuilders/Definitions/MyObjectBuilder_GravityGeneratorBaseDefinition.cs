using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GravityGeneratorBaseDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public float MinGravityAcceleration = -9.81f;
        [ProtoMember]
        public float MaxGravityAcceleration = 9.81f;
    }
}
