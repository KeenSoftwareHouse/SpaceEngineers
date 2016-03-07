using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game
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
