using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_RadioAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
        [ProtoMember]
        public bool ShowShipName;
        [ProtoMember]
        public bool EnableBroadcasting = true;
    }
}
