using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerPositionReached : MyObjectBuilder_Trigger
    {
        [ProtoMember(1)]
        public Vector3D Pos;
        [ProtoMember(2)]
        public double Distance2;
    }
}
