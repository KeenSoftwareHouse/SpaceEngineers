using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AutopilotClipboard : MyObjectBuilder_Base
    {
        [ProtoMember]
        public long RemoteEntityId;

        [ProtoMember]
        public int FlightMode;

        [ProtoMember]
        public List<int> indexes;

        [ProtoMember]
        public List<MyObjectBuilder_AutopilotWaypoint> Waypoints;

        [ProtoMember]
        public byte Direction = 0;

        [ProtoMember]
        public bool DockingModeEnabled = false;
    }
}
