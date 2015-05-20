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
        [ProtoMember]
        public ulong SteamId;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public bool IsAdmin;
    }
}
