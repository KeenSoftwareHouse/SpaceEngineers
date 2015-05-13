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
    public class MyObjectBuilder_FloatingObject : MyObjectBuilder_EntityBase
    {
        [ProtoMember(1)]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember(2), DefaultValue(null)]
        public string OreSubtypeId;
    }
}
