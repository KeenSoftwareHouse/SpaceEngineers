using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RespawnShipDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public string Prefab;

        [ProtoMember(2)]
        public int CooldownSeconds;
    }
}
