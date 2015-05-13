using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CryoChamber : MyObjectBuilder_Cockpit
    {
        [ProtoMember(1)]
        public ulong? SteamId = null;

        [ProtoMember(2)]
        public int? SerialId = null;
    }
}
