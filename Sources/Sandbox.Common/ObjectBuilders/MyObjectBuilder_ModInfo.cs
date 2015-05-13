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
    public class MyObjectBuilder_ModInfo : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public ulong SteamIDOwner;

        [ProtoMember(2)]
        public ulong WorkshopId;
    }
}
