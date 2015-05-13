using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medieval.Entities
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Rope : MyObjectBuilder_EntityBase
    {
        [ProtoMember(1)]
        public float MaxRopeLength;

        [ProtoMember(2)]
        public float CurrentRopeLength;

        [ProtoMember(3)]
        public long EntityIdHookA;

        [ProtoMember(4)]
        public long EntityIdHookB;
    }
}
