using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedicalRoom : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public ulong SteamUserId;

        [ProtoMember(2)]
        public string IdleSound;

        [ProtoMember(3)]
        public string ProgressSound;
    }
}
