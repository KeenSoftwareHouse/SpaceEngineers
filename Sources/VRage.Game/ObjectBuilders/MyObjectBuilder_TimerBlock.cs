﻿using ProtoBuf;
using System.ComponentModel;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TimerBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember, DefaultValue(false)]
        public bool JustTriggered = false;

        [ProtoMember]
        public int Delay = 10 * 1000;

        [ProtoMember]
        public int CurrentTime;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
