using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityGeneratorSphereDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float MinRadius;
        [ProtoMember(2)]
        public float MaxRadius;
        [ProtoMember(3)]
        public float BasePowerInput;
        [ProtoMember(4)]
        public float ConsumptionPower;
    }
}
