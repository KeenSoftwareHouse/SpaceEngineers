using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Cockpit : MyObjectBuilder_ShipController
    {
        [ProtoMember]
        public MyObjectBuilder_Character Pilot;

        [ProtoMember]
        public MyPositionAndOrientation? PilotRelativeWorld;

        [ProtoMember]
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
