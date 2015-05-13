using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SoundBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float Range = 50;

        [ProtoMember(2)]
        public float Volume = 1;

        [ProtoMember(3)]
        public int CueId = 0;

        [ProtoMember(4)]
        public float LoopPeriod = 1f;
    }
}
