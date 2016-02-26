using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryAggregate : MyObjectBuilder_InventoryBase
    {
        [ProtoMember, DefaultValue(null)]
        [DynamicObjectBuilderItem]
        [Serialize(MyObjectFlags.Nullable)]
        public List<MyObjectBuilder_InventoryBase> Inventories = null;

        public override void Clear()
        {
            foreach (var inv in Inventories)
                inv.Clear();
        }

    }
}
