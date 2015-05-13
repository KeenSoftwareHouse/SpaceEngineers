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
        [ProtoMember(1)]
        public float Minimum = 0f;

        [ProtoMember(2)]
        public float Maximum = 10f;

        [ProtoMember(3)]
        public string TopPart;

        [ProtoMember(4)]
        public float MaxVelocity = 5;

        [ProtoMember(5)]
        public float RequiredPowerInput;
    }
}
