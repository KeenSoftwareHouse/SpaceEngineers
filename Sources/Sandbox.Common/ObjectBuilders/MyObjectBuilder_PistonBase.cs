using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PistonBase : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float Velocity = -0.1f;

        [ProtoMember(2)]
        public float? MaxLimit;

        [ProtoMember(3)]
        public float? MinLimit;

        [ProtoMember(4)]
        public bool Reverse;

        [ProtoMember(5)]
        public long TopBlockId;

        [ProtoMember(6)]
        public float CurrentPosition;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (TopBlockId != 0) TopBlockId = remapHelper.RemapEntityId(TopBlockId);
        }
    }
}
