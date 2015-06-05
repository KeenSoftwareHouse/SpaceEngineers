﻿using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SimpleAutopilot : MyObjectBuilder_AutopilotBase
    {
        [ProtoMember]
        public Vector3D Destination;

        [ProtoMember]
        public Vector3 Direction;
    }
}
