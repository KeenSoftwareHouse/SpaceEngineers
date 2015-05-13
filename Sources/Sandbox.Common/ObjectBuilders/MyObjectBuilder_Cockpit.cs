using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Cockpit : MyObjectBuilder_ShipController
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Character Pilot;

        [ProtoMember(2)]
        public MyPositionAndOrientation? PilotRelativeWorld;

        [ProtoMember(3)]
        public MyObjectBuilder_AutopilotBase Autopilot;

        public void ClearPilotAndAutopilot()
        {
            Pilot = null;
            Autopilot = null;
        }

        [ProtoMember(4)]
        public SerializableDefinitionId? PilotGunDefinition;

        [ProtoMember(5)]
        public bool IsInFirstPersonView;

        [ProtoMember(6)]
        public float OxygenLevel;

        public override void SetupForProjector()
        {
            OxygenLevel = 0f;
            base.SetupForProjector();
        }
    }
}
