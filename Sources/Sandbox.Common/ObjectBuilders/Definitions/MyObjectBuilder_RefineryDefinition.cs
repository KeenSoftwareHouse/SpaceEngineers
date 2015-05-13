using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RefineryDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember(1)]
        public float RefineSpeed;

        [ProtoMember(2)]
        public float MaterialEfficiency;
    }
}
