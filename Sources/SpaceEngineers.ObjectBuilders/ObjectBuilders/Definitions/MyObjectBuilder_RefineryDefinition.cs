using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_RefineryDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float RefineSpeed;

        [ProtoMember]
        public float MaterialEfficiency;
    }
}
