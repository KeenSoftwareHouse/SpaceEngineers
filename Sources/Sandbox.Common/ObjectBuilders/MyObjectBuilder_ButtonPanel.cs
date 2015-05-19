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
        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember]
        public bool AnyoneCanUse;

        [ProtoMember]
        public SerializableDictionary<int, String> CustomButtonNames;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
