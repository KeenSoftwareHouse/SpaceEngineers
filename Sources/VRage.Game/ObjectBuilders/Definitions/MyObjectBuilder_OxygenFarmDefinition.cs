using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenFarmDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public Vector3 PanelOrientation = new Vector3(0, 0, 0);

        [ProtoMember]
        public bool TwoSidedPanel = true;

        [ProtoMember]
        public float PanelOffset = 1;

        [ProtoMember]
        public float MaxOxygenOutput = 0f;

        [ProtoMember]
        public float OperationalPowerConsumption = 0.001f;
    }
}
