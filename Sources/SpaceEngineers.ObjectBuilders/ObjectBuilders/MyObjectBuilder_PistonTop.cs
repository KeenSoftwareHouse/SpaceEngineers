using ProtoBuf;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_PistonTop : MyObjectBuilder_CubeBlock
    {
    }
}
