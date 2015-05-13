using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProductionBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float InventoryMaxVolume;

        [ProtoMember(2)]
        public Vector3 InventorySize;

        [ProtoMember(3)]
        public float StandbyPowerConsumption;

        [ProtoMember(4)]
        public float OperationalPowerConsumption;

        [ProtoMember(5), XmlArrayItem("Class")]
        public string[] BlueprintClasses;
    }
}
