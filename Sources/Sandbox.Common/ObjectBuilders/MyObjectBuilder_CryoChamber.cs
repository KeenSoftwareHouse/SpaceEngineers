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
        [ProtoMember]
        public ulong? SteamId = null;

        [ProtoMember]
        public int? SerialId = null;
    }
}
