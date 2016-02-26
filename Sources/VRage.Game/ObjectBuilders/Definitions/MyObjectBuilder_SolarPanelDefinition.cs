using ProtoBuf;
using VRageMath;
using VRage.ObjectBuilders;

namespace VRage.Game
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SolarPanelDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public Vector3 PanelOrientation = new Vector3(0, 0, 0);

        [ProtoMember]
        public bool TwoSidedPanel = true;

        [ProtoMember]
        public float PanelOffset = 1;
    }
}
