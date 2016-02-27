using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_WheelBlock : MyObjectBuilder_CubeBlock
    {
    }
}
