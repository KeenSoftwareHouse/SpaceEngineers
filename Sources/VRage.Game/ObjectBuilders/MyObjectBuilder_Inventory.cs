using ProtoBuf;
using VRage.Game.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [Flags]
    public enum MyInventoryFlags
    {
        CanReceive = 1,
        CanSend = 2
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Inventory : MyObjectBuilder_InventoryBase
    {
        [ProtoMember]
        public List<MyObjectBuilder_InventoryItem> Items = new List<MyObjectBuilder_InventoryItem>();

        [ProtoMember]
        public uint nextItemId;

        [ProtoMember, DefaultValue(null)]
        public MyFixedPoint? Volume = null;

        [ProtoMember, DefaultValue(null)]
        public MyFixedPoint? Mass = null;

        [ProtoMember, DefaultValue(null)]
        public int? MaxItemCount = null;
        public bool ShouldSerializeMaxItemCount() { return MaxItemCount.HasValue; }

        [ProtoMember, DefaultValue(null)]
        public SerializableVector3? Size = null;

        [ProtoMember, DefaultValue(null)]
        public MyInventoryFlags? InventoryFlags = null;
        
        [ProtoMember]
        public bool RemoveEntityOnEmpty;

        public override void Clear()
        {
            Items.Clear();
            nextItemId = 0;
            base.Clear();
        }
    }
}