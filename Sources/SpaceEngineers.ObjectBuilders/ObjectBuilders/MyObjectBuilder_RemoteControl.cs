using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.Game;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Serialization;
using VRage.Game.ObjectBuilders.AI;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_RemoteControl : MyObjectBuilder_ShipController
    {
        public const float DEFAULT_AUTOPILOT_SPEED_LIMIT = 120;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public long? PreviousControlledEntityId = null;

        [ProtoMember]
        public bool AutoPilotEnabled;

        [ProtoMember]
        public int FlightMode;

        [ProtoMember]
        public long BindedCamera = 0;

        [ProtoMember]
        public int CurrentWaypointIndex = -1;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public List<MyObjectBuilder_AutopilotWaypoint> Waypoints;

        [ProtoMember]
        public byte Direction = 0;

        [ProtoMember]
        public bool DockingModeEnabled = false;

        [ProtoMember]
        public bool CollisionAvoidance = false;

        [ProtoMember]
        [Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]
        public MyObjectBuilder_AutomaticBehaviour AutomaticBehaviour = null;
        
        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public List<Vector3D> Coords = null;

        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public List<string> Names = null;

        /// <summary>
        /// Obsolete. Use Waypoints instead.
        /// </summary>
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Toolbar AutoPilotToolbar = null;

        [ProtoMember, DefaultValue(DEFAULT_AUTOPILOT_SPEED_LIMIT)]
        public float AutopilotSpeedLimit = DEFAULT_AUTOPILOT_SPEED_LIMIT;

        [ProtoMember]
        public float WaypointThresholdDistance;

        [ProtoMember]
        public bool IsMainRemoteControl = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (AutoPilotToolbar != null)
                AutoPilotToolbar.Remap(remapHelper);

            if (Waypoints != null)
            {
                foreach (var waypoint in Waypoints)
                {
                    waypoint.Remap(remapHelper);
                }
            }
        }
    }
}
