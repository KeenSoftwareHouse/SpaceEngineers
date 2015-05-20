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
        [ProtoMember]
        public float MaxRopeLength;

        [ProtoMember]
        public float CurrentRopeLength;

        [ProtoMember]
        public long EntityIdHookA;

        [ProtoMember]
        public long EntityIdHookB;
    }
}
