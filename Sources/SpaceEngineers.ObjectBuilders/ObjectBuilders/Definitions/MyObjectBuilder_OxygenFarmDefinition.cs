using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_OxygenFarmDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoContract]
        public struct MyProducedGasInfo
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public float MaxOutputPerSecond;
        }
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public string ResourceSourceGroup;

        [ProtoMember]
        public Vector3 PanelOrientation = new Vector3(0, 0, 0);

        [ProtoMember]
        public bool TwoSidedPanel = true;

        [ProtoMember]
        public float PanelOffset = 1;

        [ProtoMember]
        public MyProducedGasInfo ProducedGas;

        [ProtoMember]
        public float OperationalPowerConsumption = 0.001f;
    }
}
