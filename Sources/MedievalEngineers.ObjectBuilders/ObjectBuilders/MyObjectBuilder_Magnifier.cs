using ProtoBuf;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Magnifier : MyObjectBuilder_EntityBase
    {
    }
}
