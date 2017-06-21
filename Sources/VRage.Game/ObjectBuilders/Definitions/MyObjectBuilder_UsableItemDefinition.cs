using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UsableItemDefinition: MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoMember]
        public string UseSound;
    }
}
