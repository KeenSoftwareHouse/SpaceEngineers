using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AssemblerDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float AssemblySpeed = 1.0f;
    }
}
