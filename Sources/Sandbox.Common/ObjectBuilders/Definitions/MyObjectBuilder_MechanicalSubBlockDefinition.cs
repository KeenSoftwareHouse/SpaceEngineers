using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Medieval.ObjectBuilders.Definitions
{
    /// <summary>
    /// Definition for common subblock mechanical parts.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MechanicalSubBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public SerializableVector3 PivotOffset;

        [ProtoMember, DefaultValue(25000f)]
        public float BreakableThreshold = 25000f;
    }
}
