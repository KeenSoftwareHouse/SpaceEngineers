using ProtoBuf;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RemoteControl : MyObjectBuilder_ShipController
    {
        [ProtoMember(1)]
        public long? PreviousControlledEntityId = null;

        [ProtoMember(2)]
        public bool AutoPilotEnabled;

        [ProtoMember(3)]
        public int FlightMode;

        [ProtoMember(4)]
        public List<Vector3D> Coords;

        [ProtoMember(5)]
        public List<string> Names;

        [ProtoMember(6)]
        public int CurrentWaypointIndex = -1;

        [ProtoMember(7)]
        public MyObjectBuilder_Toolbar AutoPilotToolbar;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (AutoPilotToolbar != null)
                AutoPilotToolbar.Remap(remapHelper);
        }
    }
}
