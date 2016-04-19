using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ShipControllerDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public bool EnableFirstPerson;
        [ProtoMember]
        public bool EnableShipControl;
        [ProtoMember]
        public bool EnableBuilderCockpit;

        [ProtoMember]
        public string GetInSound;
        [ProtoMember]
        public string GetOutSound;
    }
}
