using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Client : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)]
        public string Name;

        [ProtoMember(3)]
        public bool IsAdmin;
    }
}
