using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CraftingComponentBlock : MyObjectBuilder_CraftingComponentBase
    {
        [ProtoMember]
        public List<MyObjectBuilder_InventoryItem> InsertedItems = new List<MyObjectBuilder_InventoryItem>();

        [ProtoMember]
        public float InsertedItemUseLevel = 0f;
    }
}
