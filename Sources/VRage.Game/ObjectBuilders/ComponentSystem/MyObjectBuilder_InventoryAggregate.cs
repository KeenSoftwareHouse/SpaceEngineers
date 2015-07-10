using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryAggregate : MyObjectBuilder_InventoryBase
    {
        [ProtoMember, DefaultValue(null)]
        public List<MyObjectBuilder_InventoryBase> Inventories = null;
    }
}
