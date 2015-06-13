using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MechanicalWheelBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1), DefaultValue(0.125f)]
        public float Radius = 0.125f;
    }
}
