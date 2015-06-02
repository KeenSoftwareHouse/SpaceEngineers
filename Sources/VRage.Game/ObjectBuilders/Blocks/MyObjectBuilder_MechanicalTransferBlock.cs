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
        [ProtoContract]
        public struct MyMechanicalSubBlockData
        {
            [ProtoMember]
            public string SubBlockName;

            [ProtoMember]
            public MyMechanicalData MechanicalData;
        }

        [ProtoContract]
        public struct MyMechanicalData
        {
            [ProtoMember]
            public float MaxFrictionTorque;
        }

        [XmlArrayItem("MechanicalData")]
        [ProtoMember]
        public MyMechanicalSubBlockData[] MechanicalSubBlockData;
    }
}
