using ProtoBuf;
using VRage.ObjectBuilders;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MechanicalTransferBlock : MyObjectBuilder_CubeBlock
    {
    }
}
