using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ButtonPanel : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember(2)]
        public bool AnyoneCanUse;

        [ProtoMember(3)]
        public SerializableDictionary<int, String> CustomButtonNames;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
