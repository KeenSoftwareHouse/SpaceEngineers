using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipBlueprintDefinition : MyObjectBuilder_PrefabDefinition
    {
        [ProtoMember]
        public ulong WorkshopId;

        [ProtoMember]
        public ulong OwnerSteamId;

        [ProtoMember]
        public ulong BattlePoints;
    }
}
