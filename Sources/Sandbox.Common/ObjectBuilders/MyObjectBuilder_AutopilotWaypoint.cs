using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AutopilotWaypoint : MyObjectBuilder_Base
    {
        [ProtoMember]
        public Vector3D Coords;

        [ProtoMember]
        public string Name;

        [ProtoMember, DefaultValue(null)]
        public List<MyObjectBuilder_ToolbarItem> Actions = null;
    }
}
