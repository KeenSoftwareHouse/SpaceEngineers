using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorBase : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public long RotorEntityId;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (RotorEntityId != 0) RotorEntityId = remapHelper.RemapEntityId(RotorEntityId);
        }
    }
}
