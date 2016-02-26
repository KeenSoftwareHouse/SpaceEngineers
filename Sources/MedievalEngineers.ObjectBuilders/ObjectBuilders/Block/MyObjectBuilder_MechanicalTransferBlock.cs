using ProtoBuf;
using VRage.ObjectBuilders;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MechanicalTransferBlock : MyObjectBuilder_CubeBlock
    {
    }
}
