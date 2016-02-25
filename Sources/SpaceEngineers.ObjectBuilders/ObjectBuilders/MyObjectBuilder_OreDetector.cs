using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_OreDetector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float DetectionRadius;

        [ProtoMember]
        public bool BroadcastUsingAntennas = true;
    }
}
