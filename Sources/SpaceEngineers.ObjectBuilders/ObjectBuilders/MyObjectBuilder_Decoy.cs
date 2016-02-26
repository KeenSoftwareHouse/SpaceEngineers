using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Decoy : MyObjectBuilder_FunctionalBlock
    {
    }
}
