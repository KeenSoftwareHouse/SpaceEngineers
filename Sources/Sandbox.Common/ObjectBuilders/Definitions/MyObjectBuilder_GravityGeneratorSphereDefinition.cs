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
        [ProtoMember]
        public float MinRadius;
        [ProtoMember]
        public float MaxRadius;
        [ProtoMember]
        public float BasePowerInput;
        [ProtoMember]
        public float ConsumptionPower;
    }
}
