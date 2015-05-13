using ProtoBuf;
using System.Collections.Generic;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Inventory : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public List<MyObjectBuilder_InventoryItem> Items = new List<MyObjectBuilder_InventoryItem>();

        [ProtoMember(2)]
        public uint nextItemId;

        internal void Clear()
        {
            Items.Clear();
            nextItemId = 0;
        }
    }
}
