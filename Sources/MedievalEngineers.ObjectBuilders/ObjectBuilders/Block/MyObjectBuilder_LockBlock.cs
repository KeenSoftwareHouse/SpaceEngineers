using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LockBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public int State;
    }
}
