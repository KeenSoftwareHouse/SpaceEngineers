using ProtoBuf;
using VRage;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Cockpit : MyObjectBuilder_ShipController
    {
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Character Pilot;

        [ProtoMember]
        public MyPositionAndOrientation? PilotRelativeWorld;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable | MyObjectFlags.Dynamic ,DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]
        public MyObjectBuilder_AutopilotBase Autopilot;

        public void ClearPilotAndAutopilot()
        {
            Pilot = null;
            Autopilot = null;
        }

        [ProtoMember]
        public SerializableDefinitionId? PilotGunDefinition;

        [ProtoMember]
        public bool IsInFirstPersonView;

        [ProtoMember]
        public float OxygenLevel;

        public override void SetupForProjector()
        {
            OxygenLevel = 0f;
            base.SetupForProjector();
        }
    }
}
