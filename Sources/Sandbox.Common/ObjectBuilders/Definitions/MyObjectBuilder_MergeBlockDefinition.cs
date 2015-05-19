using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MergeBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float Strength = 0.1f;

    }
}
