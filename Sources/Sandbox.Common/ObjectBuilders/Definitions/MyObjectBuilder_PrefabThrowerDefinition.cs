using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PrefabThrowerDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public float? Mass = null;

        [ProtoMember(2)]
        public float MaxSpeed = 80; //m/sec

        [ProtoMember(3)]
        public float MinSpeed = 1; //m/sec

        [ProtoMember(4)]
        public float PushTime = 1; //sec

        [ProtoMember(5)]
        public string PrefabToThrow;

        [ProtoMember(6)]
        public string ThrowSound;
    }
}
