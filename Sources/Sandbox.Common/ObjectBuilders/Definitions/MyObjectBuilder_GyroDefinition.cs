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
    public class MyObjectBuilder_GyroDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float ForceMagnitude;
        [ProtoMember]
        public float RequiredPowerInput;
    }
}
