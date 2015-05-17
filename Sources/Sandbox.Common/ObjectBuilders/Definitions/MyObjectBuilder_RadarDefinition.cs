using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RadarDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)] 
        public float MaximumRange;
    }
}
