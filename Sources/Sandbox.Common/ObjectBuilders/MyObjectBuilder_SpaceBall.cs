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
        [ProtoMember]
        public float VirtualMass = 100;

        [ProtoMember]
        public float Friction = 0.5f;

        [ProtoMember]
        public float Restitution = 0.5f;

        [ProtoMember]
        public bool EnableBroadcast = true;
    }
}
