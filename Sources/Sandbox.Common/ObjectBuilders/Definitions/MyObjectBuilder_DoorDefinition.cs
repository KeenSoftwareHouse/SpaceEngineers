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
    public class MyObjectBuilder_DoorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float MaxOpen;

        [ProtoMember(2)]
        public string OpenSound;

        [ProtoMember(3)]
        public string CloseSound;

        [ProtoMember(4)]
        public float OpeningSpeed = 1f;
    }
}
