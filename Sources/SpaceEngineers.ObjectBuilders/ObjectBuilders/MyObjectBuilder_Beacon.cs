using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Beacon : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
    }
}
