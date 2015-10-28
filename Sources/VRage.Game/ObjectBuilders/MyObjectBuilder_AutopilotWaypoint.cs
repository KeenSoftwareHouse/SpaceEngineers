﻿using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
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
        [Serialize(MyObjectFlags.Nullable)]
        public List<MyObjectBuilder_ToolbarItem> Actions = null;

        // Used only when sending over network because ProtoBuf cannot handle nulls in array
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public List<int> Indexes = null;

        public void Remap(IMyRemapHelper remapHelper)
        {
            if (Actions != null)
            {
                foreach (var action in Actions)
                {
                    if (action != null)
                    {
                        action.Remap(remapHelper);
                    }
                }
            }
        }
    }
}
