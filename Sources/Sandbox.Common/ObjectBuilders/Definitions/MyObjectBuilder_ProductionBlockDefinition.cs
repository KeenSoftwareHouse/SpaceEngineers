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
        [ProtoMember]
        public float InventoryMaxVolume;

        [ProtoMember]
        public Vector3 InventorySize;

        [ProtoMember]
        public float StandbyPowerConsumption;

        [ProtoMember]
        public float OperationalPowerConsumption;

        [ProtoMember, XmlArrayItem("Class")]
        public string[] BlueprintClasses;
    }
}
