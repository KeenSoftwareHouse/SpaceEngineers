using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TimerBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember(2), DefaultValue(false)]
        public bool JustTriggered = false;

        [ProtoMember(3)]
        public int Delay = 10 * 1000;

        [ProtoMember(4)]
        public int CurrentTime;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
