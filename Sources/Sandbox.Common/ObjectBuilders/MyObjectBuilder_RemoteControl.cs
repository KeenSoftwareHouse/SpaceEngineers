using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RemoteControl : MyObjectBuilder_ShipController
    {
        [ProtoMember]
        public long? PreviousControlledEntityId = null;

        [ProtoMember]
        public bool AutoPilotEnabled;

        [ProtoMember]
        public int FlightMode;


        [ProtoMember]
        public int CurrentWaypointIndex = -1;

        [ProtoMember]
        public List<MyObjectBuilder_AutopilotWaypoint> Waypoints;

        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        public List<Vector3D> Coords = null;

        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        public List<string> Names = null;

        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_Toolbar AutoPilotToolbar = null;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (AutoPilotToolbar != null)
                AutoPilotToolbar.Remap(remapHelper);
        }
    }
}
