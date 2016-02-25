using VRage.ObjectBuilders;
using VRageMath;
using System.Xml.Serialization;
using ProtoBuf;

namespace VRage.Game
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
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float StandbyPowerConsumption;

        [ProtoMember]
        public float OperationalPowerConsumption;

        [ProtoMember, XmlArrayItem("Class")]
        public string[] BlueprintClasses;
    }
}
