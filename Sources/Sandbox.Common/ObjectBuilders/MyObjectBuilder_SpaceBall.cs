using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SpaceBall : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float VirtualMass = 100;

        [ProtoMember(2)]
        public float Friction = 0.5f;

        [ProtoMember(3)]
        public float Restitution = 0.5f;

        [ProtoMember(4)]
        public bool EnableBroadcast = true;
    }
}
