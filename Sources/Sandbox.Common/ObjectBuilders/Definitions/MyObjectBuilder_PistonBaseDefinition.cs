using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PistonBaseDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float Minimum = 0f;

        [ProtoMember]
        public float Maximum = 10f;

        [ProtoMember]
        public string TopPart;

        [ProtoMember]
        public float MaxVelocity = 5;

        [ProtoMember]
        public float RequiredPowerInput;
    }
}
